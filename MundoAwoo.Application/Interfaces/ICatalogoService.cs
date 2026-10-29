using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Application.Interfaces;

public interface ICatalogoService
{
    Categoria CrearCategoria(string nombre, TipoCategoria tipo);
    ItemDeAvatar CrearItem(string nombre, Categoria categoria, RanuraAvatar ranura, string recursoVisual);
    bool PermiteRanura(TipoCategoria tipo, RanuraAvatar ranura);
}
