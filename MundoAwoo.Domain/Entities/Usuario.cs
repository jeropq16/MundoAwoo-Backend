namespace MundoAwoo.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public DateTimeOffset FechaRegistro { get; set; }
    public PerfilUsuario Perfil { get; set; } = null!;
    public string? Correo { get; set; }
    public string? PasswordHash { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHastaUtc { get; set; }
    public bool EsAdministrador { get; set; }
    public ICollection<ItemDesbloqueado> ItemsDesbloqueados { get; set; } = new List<ItemDesbloqueado>();
}
