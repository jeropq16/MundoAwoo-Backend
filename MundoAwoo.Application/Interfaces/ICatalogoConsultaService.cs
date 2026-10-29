using MundoAwoo.Application.DTOs.Catalogo;

namespace MundoAwoo.Application.Interfaces;

public interface ICatalogoConsultaService
{
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemCatalogoDto>> ObtenerItemsAsync(Guid categoriaId,
        CancellationToken cancellationToken = default);
}
