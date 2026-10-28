using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MundoAwoo.Application.DTOs.Autenticacion;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Infrastructure.Persistence;

namespace MundoAwoo.Infrastructure.Identity;

// Está en Infrastructure porque utiliza Identity y una transacción de EF Core.
// Application solo conoce IAutenticacionService y sus DTOs.
public class AutenticacionService : IAutenticacionService
{
    private readonly UserManager<UsuarioIdentity> _cuentas;
    private readonly SignInManager<UsuarioIdentity> _acceso;
    private readonly AwoorldDbContext _contexto;
    private readonly IJwtTokenService _tokens;

    public AutenticacionService(UserManager<UsuarioIdentity> cuentas, SignInManager<UsuarioIdentity> acceso,
        AwoorldDbContext contexto, IJwtTokenService tokens)
    {
        _cuentas = cuentas;
        _acceso = acceso;
        _contexto = contexto;
        _tokens = tokens;
    }

    public async Task<AutenticacionRespuestaDto> RegistrarAsync(
        RegistroDto datos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();
        string correo = NormalizarCorreo(datos.Correo);
        ValidarContrasena(datos.Contrasena);
        // El constructor de dominio valida el nombre y crea el perfil.
        var usuario = new Usuario(datos.NombreVisible);
        var avatar = new Avatar(usuario.Id);

        if (await _cuentas.FindByEmailAsync(correo) != null)
            throw new CorreoYaRegistradoException();

        try
        {
            // CreateAsync de Identity guarda usando ESTE mismo contexto.
            // La transacción externa evita dejar registros parciales si algo posterior falla.
            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);
            _contexto.Usuarios.Add(usuario);
            _contexto.Avatares.Add(avatar);
            var cuenta = new UsuarioIdentity
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                UserName = correo,
                Email = correo,
                LockoutEnabled = true
            };

            // Identity genera el hash y nunca guarda la contraseña en texto plano.
            IdentityResult resultado = await _cuentas.CreateAsync(cuenta, datos.Contrasena);
            if (!resultado.Succeeded)
            {
                if (resultado.Errors.Any(error => error.Code == "DuplicateEmail" || error.Code == "DuplicateUserName"))
                    throw new CorreoYaRegistradoException();

                string mensaje = string.Join(" ", resultado.Errors.Select(TraducirError));
                throw new SolicitudInvalidaException(mensaje);
            }

            cancellationToken.ThrowIfCancellationRequested();
            AutenticacionRespuestaDto respuesta = _tokens.Crear(usuario.Id);
            await transaccion.CommitAsync(cancellationToken);
            return respuesta;
        }
        catch (DbUpdateException)
        {
            // Al salir del bloque anterior, la transacción no confirmada se revierte.
            // Otro registro concurrente pudo ocupar el correo después de nuestra consulta inicial.
            _contexto.ChangeTracker.Clear();
            if (await _cuentas.FindByEmailAsync(correo) != null)
                throw new CorreoYaRegistradoException();

            // No ocultamos otros errores de persistencia como si fueran un correo duplicado.
            throw;
        }
    }

    public async Task<AutenticacionRespuestaDto> LoginAsync(
        LoginDto datos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();
        string correo = NormalizarCorreo(datos.Correo);
        ValidarContrasena(datos.Contrasena);

        UsuarioIdentity? cuenta = await _cuentas.FindByEmailAsync(correo);
        if (cuenta == null || cuenta.TwoFactorEnabled)
            throw new CredencialesInvalidasException();

        // Comprueba contraseña y bloqueo, pero no crea una cookie ni emite un token por su cuenta.
        // lockoutOnFailure cuenta los fallos y bloquea temporalmente al alcanzar el límite.
        SignInResult resultado = await _acceso.CheckPasswordSignInAsync(cuenta, datos.Contrasena, lockoutOnFailure: true);
        if (!resultado.Succeeded)
            throw new CredencialesInvalidasException();

        cancellationToken.ThrowIfCancellationRequested();
        return _tokens.Crear(cuenta.UsuarioId);
    }

    private static string NormalizarCorreo(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            throw new SolicitudInvalidaException("El correo es obligatorio.");

        string normalizado = correo.Trim();
        if (normalizado.Length > 256 || !new EmailAddressAttribute().IsValid(normalizado))
            throw new SolicitudInvalidaException("Introduce un correo válido de hasta 256 caracteres.");

        // Identity normaliza mayúsculas/minúsculas al buscar; no modificamos la contraseña.
        return normalizado;
    }

    private static void ValidarContrasena(string contrasena)
    {
        if (string.IsNullOrEmpty(contrasena) || contrasena.Length > 128)
            throw new SolicitudInvalidaException("La contraseña es obligatoria y admite hasta 128 caracteres.");
        // La fortaleza de una contraseña nueva la valida Identity, no el login.
    }

    private string TraducirError(IdentityError error)
    {
        switch (error.Code)
        {
            case "PasswordTooShort": return $"La contraseña debe tener al menos {_cuentas.Options.Password.RequiredLength} caracteres.";
            case "PasswordRequiresDigit": return "La contraseña debe incluir un número.";
            case "PasswordRequiresLower": return "La contraseña debe incluir una minúscula.";
            case "PasswordRequiresUpper": return "La contraseña debe incluir una mayúscula.";
            case "PasswordRequiresNonAlphanumeric": return "La contraseña debe incluir un símbolo.";
            case "PasswordRequiresUniqueChars": return "La contraseña debe incluir más caracteres distintos.";
            case "InvalidEmail":
            case "InvalidUserName": return "El correo no tiene un formato permitido.";
            default: return "No se pudo crear la cuenta con los datos enviados.";
        }
    }
}
