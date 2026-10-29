using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.ValueObjects;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Application.Services;

public class AvatarService : IAvatarService
{
    private readonly IUsuarioService _usuarios;

    public AvatarService(IUsuarioService usuarios) => _usuarios = usuarios;

    public Avatar Crear(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El avatar debe pertenecer a un usuario.", nameof(usuarioId));

        return new Avatar
        {
            Id = Guid.NewGuid(), UsuarioId = usuarioId,
            ColorFondo = new ColorHex("#FFFFFF")
        };
    }

    public void CambiarColorFondo(Avatar avatar, ColorHex color)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        ArgumentNullException.ThrowIfNull(color);
        avatar.ColorFondo = color;
    }

    public void Equipar(Avatar avatar, ItemDeAvatar item, Usuario propietario)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(propietario);

        if (propietario.Id != avatar.UsuarioId)
            throw new InvalidOperationException("El usuario no es el propietario del avatar.");
        if (!_usuarios.TieneItemDesbloqueado(propietario, item.Id))
            throw new InvalidOperationException("Debes desbloquear el ítem antes de equiparlo.");
        if (avatar.ItemsEquipados.Any(equipado => equipado.ItemDeAvatarId == item.Id)) return;
        if (avatar.ItemsEquipados.Any(equipado => equipado.Ranura == item.Ranura))
            throw new InvalidOperationException("La ranura está ocupada. Desequipa su ítem primero.");

        avatar.ItemsEquipados.Add(new ItemEquipado
        {
            Id = Guid.NewGuid(), AvatarId = avatar.Id,
            ItemDeAvatarId = item.Id, Ranura = item.Ranura
        });
    }

    public void Desequipar(Avatar avatar, Guid itemDeAvatarId)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        if (itemDeAvatarId == Guid.Empty)
            throw new ArgumentException("El identificador del ítem es obligatorio.", nameof(itemDeAvatarId));

        ItemEquipado? equipado = avatar.ItemsEquipados.FirstOrDefault(item => item.ItemDeAvatarId == itemDeAvatarId);
        if (equipado is not null) avatar.ItemsEquipados.Remove(equipado);
    }
}
