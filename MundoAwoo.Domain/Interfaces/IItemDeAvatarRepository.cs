using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Domain.Interfaces;

// Contrato de consulta del catálogo compartido de ítems y sus categorías.
public interface IItemDeAvatarRepository
{
    // Consulta un ítem del catálogo. Devuelve null si no existe.
    // Que exista en el catálogo no significa que el usuario lo tenga desbloqueado.
    Task<ItemDeAvatar?> ObtenerPorIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    // Filtra por Categoria.Id (Guid), no por el enum TipoCategoria.
    // Devuelve una lista vacía si no hay coincidencias, incluso si la categoría no existe.
    Task<IReadOnlyList<ItemDeAvatar>> ObtenerPorCategoriaAsync(
        Guid categoriaId, CancellationToken cancellationToken = default);

    // Permite construir el menú de categorías; devuelve una lista vacía si no hay categorías.
    // IReadOnlyList permite consultar los resultados sin agregar ni quitar elementos de la lista.
    Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync(CancellationToken cancellationToken = default);
}
