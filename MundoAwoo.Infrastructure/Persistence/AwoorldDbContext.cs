using Microsoft.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence;

// El contexto conecta nuestras entidades con sus tablas y registra cambios pendientes.
public class AwoorldDbContext : DbContext
{
    public AwoorldDbContext(DbContextOptions<AwoorldDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PerfilUsuario> PerfilesUsuario => Set<PerfilUsuario>();
    public DbSet<Avatar> Avatares => Set<Avatar>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<ItemDeAvatar> ItemsDeAvatar => Set<ItemDeAvatar>();
    public DbSet<ItemDesbloqueado> ItemsDesbloqueados => Set<ItemDesbloqueado>();
    public DbSet<ItemEquipado> ItemsEquipados => Set<ItemEquipado>();
    public DbSet<SesionRefreshToken> SesionesRefreshToken => Set<SesionRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // utf8mb4 permite nombres y biografías con tildes y emojis.
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AwoorldDbContext).Assembly);
    }
}
