using MundoAwoo.Application.DTOs.Perfiles;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Application.Services;

// Coordina el caso de uso. Las reglas siguen en Domain y el SQL en Infrastructure.
public class PerfilService : IPerfilService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unidadDeTrabajo;

    public PerfilService(IUsuarioRepository usuarios, IUnitOfWork unidadDeTrabajo)
    {
        _usuarios = usuarios;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    public async Task<PerfilUsuarioDto> ObtenerAsync(
        Guid usuarioId, CancellationToken cancellationToken = default)
    {
        Usuario usuario = await BuscarUsuarioAsync(usuarioId, cancellationToken);

        // Consultar no modifica el perfil y no necesita GuardarCambiosAsync.
        return CrearDto(usuario.Perfil);
    }

    public async Task<PerfilUsuarioDto> ActualizarAsync(
        Guid usuarioId, ActualizarPerfilDto datos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);

        Usuario usuario = await BuscarUsuarioAsync(usuarioId, cancellationToken);

        // El dominio valida longitudes, rechaza nombres vacíos y quita espacios exteriores.
        // Si la validación falla, no llegamos a guardar.
        usuario.ActualizarPerfil(datos.NombreVisible, datos.Biografia);

        // El repositorio cargó el usuario con seguimiento: no hace falta llamar a Update.
        // Esperamos el guardado antes de devolver una respuesta de éxito.
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return CrearDto(usuario.Perfil);
    }

    private async Task<Usuario> BuscarUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));

        cancellationToken.ThrowIfCancellationRequested();
        Usuario? usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);

        if (usuario == null)
            throw new UsuarioNoEncontradoException(usuarioId);

        return usuario;
    }

    private static PerfilUsuarioDto CrearDto(PerfilUsuario perfil)
    {
        // Mapeo manual: cada propiedad del DTO recibe el dato correspondiente del perfil.
        return new PerfilUsuarioDto
        {
            Id = perfil.Id,
            UsuarioId = perfil.UsuarioId,
            NombreVisible = perfil.NombreVisible,
            Biografia = perfil.Biografia
        };
    }
}
