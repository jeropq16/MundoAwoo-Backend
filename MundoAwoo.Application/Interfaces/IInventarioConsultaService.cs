using MundoAwoo.Application.DTOs.Inventario;

namespace MundoAwoo.Application.Interfaces;

public interface IInventarioConsultaService
{
    Task<IReadOnlyList<ItemInventarioDto>> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
