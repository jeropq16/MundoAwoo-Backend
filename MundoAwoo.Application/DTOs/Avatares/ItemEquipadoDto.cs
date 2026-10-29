namespace MundoAwoo.Application.DTOs.Avatares;

public class ItemEquipadoDto
{
    public Guid ItemDeAvatarId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ranura { get; set; } = string.Empty;
    public string RecursoVisual { get; set; } = string.Empty;
}
