namespace MundoAwoo.Domain.Entities;

// Raíz de agregado: controla los cambios del perfil y del inventario.
public class Usuario
{
    // EF reconstruye datos existentes sin ejecutar las reglas de creación de un objeto nuevo.
    // Es privado: el resto de la aplicación sigue usando el constructor con validaciones.
    private Usuario()
    {
        Perfil = null!; // EF asignará la relación al cargarla con Include.
    }

    private readonly List<ItemDesbloqueado> _itemsDesbloqueados = new();

    public Guid Id { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }
    public PerfilUsuario Perfil { get; private set; }

    // AsReadOnly evita que quien recibe la colección pueda modificar la lista interna.
    public IReadOnlyCollection<ItemDesbloqueado> ItemsDesbloqueados => _itemsDesbloqueados.AsReadOnly();

    public Usuario(string nombreVisible)
    {
        Id = Guid.NewGuid();
        FechaRegistro = DateTimeOffset.UtcNow;
        Perfil = new PerfilUsuario(Id, nombreVisible);
        // Correo, contraseña y login se gestionarán con Identity en Infrastructure.
    }

    public void ActualizarPerfil(string nombreVisible, string biografia)
    {
        Perfil.Actualizar(nombreVisible, biografia);
    }

    public bool TieneItemDesbloqueado(Guid itemDeAvatarId)
    {
        // Any devuelve true si encuentra al menos un registro que cumple la condición.
        return _itemsDesbloqueados.Any(item => item.ItemDeAvatarId == itemDeAvatarId);
    }

    public void DesbloquearItem(ItemDeAvatar item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // Repetir la misma recompensa no debe duplicar el inventario.
        if (TieneItemDesbloqueado(item.Id))
            return;

        _itemsDesbloqueados.Add(new ItemDesbloqueado(Id, item.Id));
    }
}
