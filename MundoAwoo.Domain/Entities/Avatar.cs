using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Domain.Entities;

public class Avatar
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Personaje3D { get; set; } = "ghetto";
    public bool ChaquetaGhetto { get; set; }
    public bool ChaquetaQueen { get; set; }
    public string PeloGhetto { get; set; } = "original";
    public string BarbaGhetto { get; set; } = "original";
    public string TatuajesGhetto { get; set; } = "ninguno";
    public string ColorCabelloGhetto { get; set; } = "#201813";
    public string PeloQueen { get; set; } = "original";
    public string BarbaQueen { get; set; } = "original";
    public string TatuajesQueen { get; set; } = "ninguno";
    public string ColorCabelloQueen { get; set; } = "#29161C";
    public ColorHex ColorFondo { get; set; } = null!;
    public ICollection<ItemEquipado> ItemsEquipados { get; set; } = new List<ItemEquipado>();
}
