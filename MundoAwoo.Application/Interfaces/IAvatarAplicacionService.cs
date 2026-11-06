using MundoAwoo.Application.DTOs.Avatares;

namespace MundoAwoo.Application.Interfaces;

public interface IAvatarAplicacionService
{
    Task<AvatarDto> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<AvatarDto> CambiarColorFondoAsync(Guid usuarioId, CambiarColorFondoDto datos,
        CancellationToken cancellationToken = default);
    Task<AvatarDto> CambiarEstilo3DAsync(Guid usuarioId, CambiarEstilo3DDto datos,
        CancellationToken cancellationToken = default);
    Task<AvatarDto> CambiarApariencia3DAsync(Guid usuarioId, CambiarApariencia3DDto datos,
        CancellationToken cancellationToken = default);
    Task<AvatarDto> EquiparAsync(Guid usuarioId, Guid itemId, CancellationToken cancellationToken = default);
    Task DesequiparAsync(Guid usuarioId, Guid itemId, CancellationToken cancellationToken = default);
}
