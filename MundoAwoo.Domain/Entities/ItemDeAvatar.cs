using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Domain.Entities;

public class ItemDeAvatar
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public RanuraAvatar Ranura { get; set; }
    public string RecursoVisual { get; set; } = string.Empty;
}
