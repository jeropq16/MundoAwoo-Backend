using MundoAwoo.Application.DTOs.Catalogo;

namespace MundoAwoo.Application.DTOs.Inventario;

public class ItemInventarioDto
{
    public ItemCatalogoDto Item { get; set; } = null!;
    public DateTimeOffset FechaDesbloqueo { get; set; }
}
