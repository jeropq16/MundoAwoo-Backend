using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using MundoAwoo.Application.DTOs.Autenticacion;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Application.Security;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Application.Services;

public class AutenticacionService : IAutenticacionService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IAvatarRepository _avatares;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokens;
    private readonly IUsuarioService _usuarioService;
    private readonly IAvatarService _avatarService;
    private readonly ISesionRefreshTokenRepository _sesiones;

    public AutenticacionService(IUsuarioRepository usuarios, IAvatarRepository avatares,
        IUnitOfWork unidadDeTrabajo, IPasswordHasher passwordHasher, IJwtTokenService tokens,
        IUsuarioService usuarioService, IAvatarService avatarService,
        ISesionRefreshTokenRepository sesiones)
    {
        _usuarios = usuarios;
        _avatares = avatares;
        _unidadDeTrabajo = unidadDeTrabajo;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _usuarioService = usuarioService;
        _avatarService = avatarService;
        _sesiones = sesiones;
    }

    public async Task<AutenticacionRespuestaDto> RegistrarAsync(
        RegistroDto datos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();
        string correo = NormalizarCorreo(datos.Correo);
        ValidarContrasenaNueva(datos.Contrasena);

        if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
            throw new CorreoYaRegistradoException();

        var usuario = _usuarioService.Crear(datos.NombreVisible);
        usuario.Correo = correo;
        usuario.PasswordHash = _passwordHasher.Hash(datos.Contrasena);
        _usuarios.Agregar(usuario);
        _avatares.Agregar(_avatarService.Crear(usuario.Id));

        // Las tres entidades se guardan en un solo SaveChanges. Si falla, no queda un registro parcial.
        AutenticacionRespuestaDto respuesta = CrearSesion(usuario.Id);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return respuesta;
    }

    public async Task<AutenticacionRespuestaDto> LoginAsync(
        LoginDto datos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();
        string correo = NormalizarCorreo(datos.Correo);
        if (string.IsNullOrEmpty(datos.Contrasena) || datos.Contrasena.Length > 128)
            throw new SolicitudInvalidaException("La contraseña es obligatoria y admite hasta 128 caracteres.");

        Usuario? usuario = await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken);
        if (usuario is null || usuario.PasswordHash is null || usuario.BloqueadoHastaUtc > DateTime.UtcNow)
            throw new CredencialesInvalidasException();

        if (!_passwordHasher.Verify(datos.Contrasena, usuario.PasswordHash))
        {
            if (usuario.BloqueadoHastaUtc <= DateTime.UtcNow)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHastaUtc = null;
            }
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= 5)
                usuario.BloqueadoHastaUtc = DateTime.UtcNow.AddMinutes(5);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
            throw new CredencialesInvalidasException();
        }

        if (usuario.IntentosFallidos > 0 || usuario.BloqueadoHastaUtc is not null)
        {
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHastaUtc = null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        AutenticacionRespuestaDto respuesta = CrearSesion(usuario.Id);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return respuesta;
    }

    public async Task<AutenticacionRespuestaDto> RefrescarAsync(RefreshTokenDto datos,
        CancellationToken cancellationToken = default)
    {
        string tokenAnterior = ValidarToken(datos);
        Guid sesionId = ObtenerSesionId(tokenAnterior);
        SesionRefreshToken sesion = await _sesiones.ObtenerPorIdAsync(sesionId, cancellationToken)
            ?? throw new RefreshTokenInvalidoException();
        DateTime ahora = DateTime.UtcNow;
        if (sesion.RevocadoEnUtc is not null || sesion.ExpiraEnUtc <= ahora ||
            !CoincideHash(sesion.TokenHash, CalcularHash(tokenAnterior)))
            throw new RefreshTokenInvalidoException();

        string tokenNuevo = GenerarToken(sesion.Id);
        AutenticacionRespuestaDto respuesta = _tokens.Crear(sesion.UsuarioId,
            new DateTimeOffset(sesion.ExpiraEnUtc, TimeSpan.Zero));
        bool rotado = await _sesiones.RotarAsync(sesion.Id, sesion.TokenHash, CalcularHash(tokenNuevo),
            ahora, cancellationToken);
        if (!rotado) throw new RefreshTokenInvalidoException();
        respuesta.RefreshToken = tokenNuevo;
        respuesta.SesionExpiraEnUtc = new DateTimeOffset(sesion.ExpiraEnUtc, TimeSpan.Zero);
        return respuesta;
    }

    public async Task CerrarSesionAsync(RefreshTokenDto datos, CancellationToken cancellationToken = default)
    {
        string token = ValidarToken(datos);
        Guid sesionId = ObtenerSesionId(token);
        SesionRefreshToken sesion = await _sesiones.ObtenerPorIdAsync(sesionId, cancellationToken)
            ?? throw new RefreshTokenInvalidoException();
        DateTime ahora = DateTime.UtcNow;
        if (sesion.RevocadoEnUtc is not null || sesion.ExpiraEnUtc <= ahora ||
            !CoincideHash(sesion.TokenHash, CalcularHash(token)) ||
            !await _sesiones.RevocarAsync(sesion.Id, sesion.TokenHash, ahora, cancellationToken))
            throw new RefreshTokenInvalidoException();
    }

    private AutenticacionRespuestaDto CrearSesion(Guid usuarioId)
    {
        DateTime expira = DateTime.UtcNow.AddHours(1);
        var sesion = new SesionRefreshToken { Id = Guid.NewGuid(), UsuarioId = usuarioId,
            ExpiraEnUtc = expira };
        string token = GenerarToken(sesion.Id);
        sesion.TokenHash = CalcularHash(token);
        AutenticacionRespuestaDto respuesta = _tokens.Crear(usuarioId,
            new DateTimeOffset(expira, TimeSpan.Zero));
        respuesta.RefreshToken = token;
        respuesta.SesionExpiraEnUtc = new DateTimeOffset(expira, TimeSpan.Zero);
        _sesiones.Agregar(sesion);
        return respuesta;
    }

    private static string ValidarToken(RefreshTokenDto datos)
    {
        if (datos is null || string.IsNullOrWhiteSpace(datos.RefreshToken) || datos.RefreshToken.Length != 97)
            throw new RefreshTokenInvalidoException();
        return datos.RefreshToken;
    }

    private static Guid ObtenerSesionId(string token)
    {
        if (token[32] != '.' || !Guid.TryParseExact(token[..32], "N", out Guid sesionId) ||
            !token[33..].All(Uri.IsHexDigit))
            throw new RefreshTokenInvalidoException();
        return sesionId;
    }

    private static string GenerarToken(Guid sesionId)
        => $"{sesionId:N}.{Convert.ToHexString(RandomNumberGenerator.GetBytes(32))}";

    private static string CalcularHash(string token)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(token)));

    private static bool CoincideHash(string esperado, string recibido)
        => CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(esperado), Convert.FromHexString(recibido));

    private static string NormalizarCorreo(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            throw new SolicitudInvalidaException("El correo es obligatorio.");

        string normalizado = correo.Trim().ToLowerInvariant();
        if (normalizado.Length > 256 || !new EmailAddressAttribute().IsValid(normalizado))
            throw new SolicitudInvalidaException("Introduce un correo válido de hasta 256 caracteres.");

        return normalizado;
    }

    private static void ValidarContrasenaNueva(string contrasena)
    {
        if (string.IsNullOrEmpty(contrasena) || contrasena.Length > 128)
            throw new SolicitudInvalidaException("La contraseña es obligatoria y admite hasta 128 caracteres.");
        if (contrasena.Length < 12 || !contrasena.Any(char.IsUpper) ||
            !contrasena.Any(char.IsLower) || !contrasena.Any(char.IsDigit) ||
            !contrasena.Any(caracter => !char.IsLetterOrDigit(caracter)))
            throw new SolicitudInvalidaException(
                "La contraseña debe tener al menos 12 caracteres, mayúscula, minúscula, número y símbolo.");
    }
}
