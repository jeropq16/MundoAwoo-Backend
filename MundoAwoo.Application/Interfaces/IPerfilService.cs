using MundoAwoo.Application.DTOs.Perfiles;

namespace MundoAwoo.Application.Interfaces;

public interface IPerfilService
{
    // usuarioId debe proceder de una identidad autenticada y autorizada por la API.
    // Si no existe, lanza UsuarioNoEncontradoException; no devuelve un perfil vacío.
    Task<PerfilUsuarioDto> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    // Reemplaza nombre visible y biografía. No es una actualización parcial (PATCH).
    // Devuelve los datos normalizados después de confirmar el guardado.
    Task<PerfilUsuarioDto> ActualizarAsync(
        Guid usuarioId, ActualizarPerfilDto datos, CancellationToken cancellationToken = default);
}
