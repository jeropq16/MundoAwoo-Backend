namespace MundoAwoo.Domain.Entities;

public class ItemDesbloqueado
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid ItemDeAvatarId { get; set; }
    public DateTimeOffset FechaDesbloqueo { get; set; }
}
