namespace MundoAwoo.Domain.Entities;

// Registra que un usuario tiene derecho a usar un ítem del catálogo.
public class ItemDesbloqueado
{
    // EF reconstruye datos existentes sin ejecutar las reglas de creación de un objeto nuevo.
    // Es privado: el resto de la aplicación sigue usando el constructor con validaciones.
    private ItemDesbloqueado()
    {
    }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid ItemDeAvatarId { get; private set; }
    public DateTimeOffset FechaDesbloqueo { get; private set; }

    internal ItemDesbloqueado(Guid usuarioId, Guid itemDeAvatarId)
    {
        if (usuarioId == Guid.Empty || itemDeAvatarId == Guid.Empty)
            throw new ArgumentException("El usuario y el ítem deben tener identificadores válidos.");

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        ItemDeAvatarId = itemDeAvatarId;
        FechaDesbloqueo = DateTimeOffset.UtcNow;
    }
}
