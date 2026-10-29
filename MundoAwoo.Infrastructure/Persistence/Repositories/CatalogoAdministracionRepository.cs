using Microsoft.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Infrastructure.Persistence.Repositories;

public class CatalogoAdministracionRepository : ICatalogoAdministracionRepository
{
    private readonly AwoorldDbContext _contexto;

    public CatalogoAdministracionRepository(AwoorldDbContext contexto) => _contexto = contexto;

    public Task<Categoria?> ObtenerCategoriaPorIdAsync(Guid categoriaId, CancellationToken cancellationToken = default)
        => _contexto.Categorias.AsNoTracking()
            .SingleOrDefaultAsync(categoria => categoria.Id == categoriaId, cancellationToken);

    public Task<bool> ExisteCategoriaConNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => _contexto.Categorias.AsNoTracking()
            .AnyAsync(categoria => categoria.Nombre.ToLower() == nombre.Trim().ToLower(), cancellationToken);

    public Task<bool> ExisteItemConNombreAsync(Guid categoriaId, string nombre,
        CancellationToken cancellationToken = default)
        => _contexto.ItemsDeAvatar.AsNoTracking()
            .AnyAsync(item => item.CategoriaId == categoriaId &&
                item.Nombre.ToLower() == nombre.Trim().ToLower(), cancellationToken);

    public void AgregarCategoria(Categoria categoria) => _contexto.Categorias.Add(categoria);
    public void AgregarItem(ItemDeAvatar item) => _contexto.ItemsDeAvatar.Add(item);
}
