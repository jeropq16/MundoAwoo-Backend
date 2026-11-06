using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Domain.Entities;

public class Avatar
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Personaje3D { get; set; } = "ghetto";
    public bool ChaquetaGhetto { get; set; }
    public bool ChaquetaQueen { get; set; }
    public ColorHex ColorFondo { get; set; } = null!;
    public ICollection<ItemEquipado> ItemsEquipados { get; set; } = new List<ItemEquipado>();
}
