using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Domain.Entities;

public class Avatar
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public ColorHex ColorFondo { get; set; } = null!;
    public ICollection<ItemEquipado> ItemsEquipados { get; set; } = new List<ItemEquipado>();
}
