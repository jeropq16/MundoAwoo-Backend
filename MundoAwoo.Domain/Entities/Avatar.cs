using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Domain.Entities;

// Raíz de agregado independiente de Usuario. Se relacionan mediante UsuarioId.
public class Avatar
{
    // EF reconstruye datos existentes sin ejecutar las reglas de creación de un objeto nuevo.
    // Es privado: el resto de la aplicación sigue usando el constructor con validaciones.
    private Avatar()
    {
        ColorFondo = null!; // EF lo reconstruirá desde la columna ColorFondo.
    }

    private readonly List<ItemEquipado> _itemsEquipados = new();

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public ColorHex ColorFondo { get; private set; }
    public IReadOnlyCollection<ItemEquipado> ItemsEquipados => _itemsEquipados.AsReadOnly();

    public Avatar(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El avatar debe pertenecer a un usuario.", nameof(usuarioId));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        ColorFondo = new ColorHex("#FFFFFF");
    }

    public void CambiarColorFondo(ColorHex color)
    {
        ArgumentNullException.ThrowIfNull(color);
        ColorFondo = color;
    }

    public void Equipar(ItemDeAvatar item, Usuario propietario)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(propietario);

        // Application cargará ambos agregados. Aquí solo leemos el inventario del propietario.
        if (propietario.Id != UsuarioId)
            throw new InvalidOperationException("El usuario no es el propietario del avatar.");

        if (!propietario.TieneItemDesbloqueado(item.Id))
            throw new InvalidOperationException("Debes desbloquear el ítem antes de equiparlo.");

        // Repetir la selección del mismo ítem no crea un segundo registro.
        if (_itemsEquipados.Any(equipado => equipado.ItemDeAvatarId == item.Id))
            return;

        if (_itemsEquipados.Any(equipado => equipado.Ranura == item.Ranura))
            throw new InvalidOperationException("La ranura está ocupada. Desequipa su ítem primero.");

        _itemsEquipados.Add(new ItemEquipado(Id, item));
    }

    public void Desequipar(Guid itemDeAvatarId)
    {
        if (itemDeAvatarId == Guid.Empty)
            throw new ArgumentException("El identificador del ítem es obligatorio.", nameof(itemDeAvatarId));

        ItemEquipado? equipado = _itemsEquipados.FirstOrDefault(item => item.ItemDeAvatarId == itemDeAvatarId);

        // Si ya estaba desequipado, el estado deseado ya se cumple.
        if (equipado != null)
            _itemsEquipados.Remove(equipado);
    }
}
