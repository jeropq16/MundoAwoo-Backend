using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Domain.Entities;

public class ItemEquipado
{
    // EF reconstruye datos existentes sin ejecutar las reglas de creación de un objeto nuevo.
    // Es privado: el resto de la aplicación sigue usando el constructor con validaciones.
    private ItemEquipado()
    {
    }

    public Guid Id { get; private set; }
    public Guid AvatarId { get; private set; }
    public Guid ItemDeAvatarId { get; private set; }
    public RanuraAvatar Ranura { get; private set; }

    internal ItemEquipado(Guid avatarId, ItemDeAvatar item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (avatarId == Guid.Empty)
            throw new ArgumentException("El avatar es obligatorio.", nameof(avatarId));

        Id = Guid.NewGuid();
        AvatarId = avatarId;
        ItemDeAvatarId = item.Id;
        // La ranura se toma del catálogo: quien equipa no puede elegir otra arbitrariamente.
        Ranura = item.Ranura;
    }
}
