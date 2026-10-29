namespace MundoAwoo.Domain.Entities;

public class PerfilUsuario
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string NombreVisible { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
}
