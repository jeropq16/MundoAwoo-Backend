using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Domain.Entities;

public class Categoria
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoCategoria Tipo { get; set; }
}
