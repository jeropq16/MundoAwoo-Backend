namespace MundoAwoo.Domain.Entities;

public class SesionRefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEnUtc { get; set; }
    public DateTime? RevocadoEnUtc { get; set; }
}
