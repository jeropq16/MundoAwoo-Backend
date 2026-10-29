using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Domain.Entities;

public class ItemEquipado
{
    public Guid Id { get; set; }
    public Guid AvatarId { get; set; }
    public Guid ItemDeAvatarId { get; set; }
    public RanuraAvatar Ranura { get; set; }
}
