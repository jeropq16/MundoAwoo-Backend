namespace MundoAwoo.Application.DTOs.Avatares;

public class AvatarDto
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Personaje3D { get; set; } = "ghetto";
    public bool ChaquetaGhetto { get; set; }
    public bool ChaquetaQueen { get; set; }
    public Apariencia3DDto AparienciaGhetto { get; set; } = new();
    public Apariencia3DDto AparienciaQueen { get; set; } = new() { ColorCabello = "#29161C" };
    public string ColorFondo { get; set; } = string.Empty;
    public IReadOnlyList<ItemEquipadoDto> ItemsEquipados { get; set; } = [];
}
