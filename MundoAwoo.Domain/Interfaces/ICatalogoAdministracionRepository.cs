using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Domain.Interfaces;

public interface ICatalogoAdministracionRepository
{
    Task<Categoria?> ObtenerCategoriaPorIdAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<bool> ExisteCategoriaConNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<bool> ExisteItemConNombreAsync(Guid categoriaId, string nombre,
        CancellationToken cancellationToken = default);
    void AgregarCategoria(Categoria categoria);
    void AgregarItem(ItemDeAvatar item);
}
