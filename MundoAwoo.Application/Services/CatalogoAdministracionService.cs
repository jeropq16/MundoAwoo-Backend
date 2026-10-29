using MundoAwoo.Application.DTOs.Catalogo;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Application.Services;

public class CatalogoAdministracionService : ICatalogoAdministracionService
{
    private readonly IAccesoAdministradorService _acceso;
    private readonly ICatalogoAdministracionRepository _catalogo;
    private readonly ICatalogoService _reglas;
    private readonly IUnitOfWork _unidadDeTrabajo;

    public CatalogoAdministracionService(IAccesoAdministradorService acceso,
        ICatalogoAdministracionRepository catalogo, ICatalogoService reglas, IUnitOfWork unidadDeTrabajo)
    {
        _acceso = acceso;
        _catalogo = catalogo;
        _reglas = reglas;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    public async Task<CategoriaDto> CrearCategoriaAsync(Guid administradorId, CrearCategoriaDto datos,
        CancellationToken cancellationToken = default)
    {
        await _acceso.ExigirAdministradorAsync(administradorId, cancellationToken);
        ArgumentNullException.ThrowIfNull(datos);
        TipoCategoria tipo = LeerEnum<TipoCategoria>(datos.Tipo, "tipo");
        Categoria categoria = _reglas.CrearCategoria(datos.Nombre, tipo);
        if (await _catalogo.ExisteCategoriaConNombreAsync(categoria.Nombre, cancellationToken))
            throw new CatalogoDuplicadoException("Ya existe una categoría con ese nombre.");
        _catalogo.AgregarCategoria(categoria);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return new CategoriaDto { Id = categoria.Id, Nombre = categoria.Nombre, Tipo = categoria.Tipo.ToString() };
    }

    public async Task<ItemCatalogoDto> CrearItemAsync(Guid administradorId, CrearItemCatalogoDto datos,
        CancellationToken cancellationToken = default)
    {
        await _acceso.ExigirAdministradorAsync(administradorId, cancellationToken);
        ArgumentNullException.ThrowIfNull(datos);
        if (datos.CategoriaId == Guid.Empty)
            throw new ArgumentException("La categoría es obligatoria.", nameof(datos));
        RanuraAvatar ranura = LeerEnum<RanuraAvatar>(datos.Ranura, "ranura");
        Categoria categoria = await _catalogo.ObtenerCategoriaPorIdAsync(datos.CategoriaId, cancellationToken)
            ?? throw new CategoriaNoEncontradaException();
        ItemDeAvatar item = _reglas.CrearItem(datos.Nombre, categoria, ranura, datos.RecursoVisual);
        if (await _catalogo.ExisteItemConNombreAsync(categoria.Id, item.Nombre, cancellationToken))
            throw new CatalogoDuplicadoException("Ya existe un ítem con ese nombre en la categoría.");
        _catalogo.AgregarItem(item);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return CatalogoConsultaService.CrearItemDto(item);
    }

    private static TEnum LeerEnum<TEnum>(string valor, string nombre) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor) ||
            !Enum.GetNames<TEnum>().Any(nombreValido =>
                string.Equals(nombreValido, valor.Trim(), StringComparison.OrdinalIgnoreCase)) ||
            !Enum.TryParse(valor.Trim(), true, out TEnum resultado))
            throw new ArgumentException($"El valor de {nombre} no existe.", nombre);
        return resultado;
    }
}
