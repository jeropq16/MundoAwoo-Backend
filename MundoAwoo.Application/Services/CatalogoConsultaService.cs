using MundoAwoo.Application.DTOs.Catalogo;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Application.Services;

public class CatalogoConsultaService : ICatalogoConsultaService
{
    private readonly IItemDeAvatarRepository _items;

    public CatalogoConsultaService(IItemDeAvatarRepository items) => _items = items;

    public async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Categoria> categorias = await _items.ObtenerCategoriasAsync(cancellationToken);
        return categorias.Select(categoria => new CategoriaDto
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            Tipo = categoria.Tipo.ToString()
        }).ToArray();
    }

    public async Task<IReadOnlyList<ItemCatalogoDto>> ObtenerItemsAsync(Guid categoriaId,
        CancellationToken cancellationToken = default)
    {
        if (categoriaId == Guid.Empty)
            throw new ArgumentException("El identificador de categoría es obligatorio.", nameof(categoriaId));

        IReadOnlyList<ItemDeAvatar> items = await _items.ObtenerPorCategoriaAsync(categoriaId, cancellationToken);
        return items.Select(CrearItemDto).ToArray();
    }

    public static ItemCatalogoDto CrearItemDto(ItemDeAvatar item) => new()
    {
        Id = item.Id,
        Nombre = item.Nombre,
        CategoriaId = item.CategoriaId,
        Ranura = item.Ranura.ToString(),
        RecursoVisual = item.RecursoVisual
    };
}
