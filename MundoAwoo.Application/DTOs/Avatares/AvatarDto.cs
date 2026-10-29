namespace MundoAwoo.Application.DTOs.Avatares;

public class AvatarDto
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string ColorFondo { get; set; } = string.Empty;
    public IReadOnlyList<ItemEquipadoDto> ItemsEquipados { get; set; } = [];
}
