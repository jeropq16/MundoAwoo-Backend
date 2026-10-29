using Microsoft.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Infrastructure.Persistence.Repositories;

public class ItemDeAvatarRepository : IItemDeAvatarRepository
{
    private readonly AwoorldDbContext _contexto;

    public ItemDeAvatarRepository(AwoorldDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<ItemDeAvatar?> ObtenerPorIdAsync(
        Guid itemId, CancellationToken cancellationToken = default)
    {
        return await _contexto.ItemsDeAvatar
            // Consultamos el catálogo sin preparar sus objetos para modificaciones.
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == itemId, cancellationToken);
    }

    public async Task<IReadOnlyList<ItemDeAvatar>> ObtenerPorIdsAsync(
        IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0) return [];

        return await _contexto.ItemsDeAvatar
            .AsNoTracking()
            .Where(item => itemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemDeAvatar>> ObtenerPorCategoriaAsync(
        Guid categoriaId, CancellationToken cancellationToken = default)
    {
        return await _contexto.ItemsDeAvatar
            .AsNoTracking()
            .Where(item => item.CategoriaId == categoriaId)
            .OrderBy(item => item.Nombre)
            // Si dos nombres coinciden, Id mantiene un orden estable entre ellos.
            .ThenBy(item => item.Id)
            // Ejecuta la consulta; devuelve una lista vacía si no hay coincidencias.
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _contexto.Categorias
            .AsNoTracking()
            .OrderBy(categoria => categoria.Nombre)
            .ThenBy(categoria => categoria.Id)
            .ToListAsync(cancellationToken);
    }
}
