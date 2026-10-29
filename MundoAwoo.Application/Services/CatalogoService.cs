using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Application.Services;

public class CatalogoService : ICatalogoService
{
    public Categoria CrearCategoria(string nombre, TipoCategoria tipo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50)
            throw new ArgumentException("El nombre debe tener entre 1 y 50 caracteres.", nameof(nombre));
        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de categoría no existe.");

        return new Categoria { Id = Guid.NewGuid(), Nombre = nombre.Trim(), Tipo = tipo };
    }

    public ItemDeAvatar CrearItem(string nombre, Categoria categoria, RanuraAvatar ranura, string recursoVisual)
    {
        ArgumentNullException.ThrowIfNull(categoria);
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100)
            throw new ArgumentException("El nombre debe tener entre 1 y 100 caracteres.", nameof(nombre));
        if (!Enum.IsDefined(ranura))
            throw new ArgumentOutOfRangeException(nameof(ranura), "La ranura no existe.");
        if (!PermiteRanura(categoria.Tipo, ranura))
            throw new ArgumentException("La ranura no corresponde al tipo de categoría.", nameof(ranura));
        if (string.IsNullOrWhiteSpace(recursoVisual) || recursoVisual.Trim().Length > 500)
            throw new ArgumentException("La referencia visual debe tener entre 1 y 500 caracteres.", nameof(recursoVisual));

        return new ItemDeAvatar
        {
            Id = Guid.NewGuid(), Nombre = nombre.Trim(), CategoriaId = categoria.Id,
            Ranura = ranura, RecursoVisual = recursoVisual.Trim()
        };
    }

    public bool PermiteRanura(TipoCategoria tipo, RanuraAvatar ranura) => tipo switch
    {
        TipoCategoria.Ropa => ranura is RanuraAvatar.Torso or RanuraAvatar.Piernas or RanuraAvatar.Pies,
        TipoCategoria.Accesorio => ranura is RanuraAvatar.Cabeza or RanuraAvatar.Ojos or RanuraAvatar.Cuello,
        TipoCategoria.Baile or TipoCategoria.Gesto => ranura == RanuraAvatar.Animacion,
        _ => false
    };
}
