using MundoAwoo.Application.DTOs.Catalogo;

namespace MundoAwoo.Application.Interfaces;

public interface ICatalogoAdministracionService
{
    Task<CategoriaDto> CrearCategoriaAsync(Guid administradorId, CrearCategoriaDto datos,
        CancellationToken cancellationToken = default);
    Task<ItemCatalogoDto> CrearItemAsync(Guid administradorId, CrearItemCatalogoDto datos,
        CancellationToken cancellationToken = default);
}
