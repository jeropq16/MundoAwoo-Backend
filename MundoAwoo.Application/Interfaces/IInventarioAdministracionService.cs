using MundoAwoo.Application.DTOs.Inventario;

namespace MundoAwoo.Application.Interfaces;

public interface IInventarioAdministracionService
{
    Task<ItemInventarioDto> ConcederAsync(Guid administradorId, Guid usuarioId, Guid itemId,
        CancellationToken cancellationToken = default);
}
