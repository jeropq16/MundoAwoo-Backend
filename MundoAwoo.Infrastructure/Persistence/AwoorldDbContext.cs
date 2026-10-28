using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Infrastructure.Identity;

namespace MundoAwoo.Infrastructure.Persistence;

// El contexto conecta nuestras entidades con sus tablas y registra cambios pendientes.
public class AwoorldDbContext : IdentityDbContext<UsuarioIdentity, IdentityRole<Guid>, Guid>
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity y Domain comparten contexto y conexión para guardar el registro en una transacción.
        modelBuilder.Entity<UsuarioIdentity>().HasOne<Usuario>().WithOne()
            .HasForeignKey<UsuarioIdentity>(cuenta => cuenta.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsuarioIdentity>().HasIndex(cuenta => cuenta.UsuarioId).IsUnique();
        // RequireUniqueEmail valida antes de insertar; este índice también protege ante concurrencia.
        modelBuilder.Entity<UsuarioIdentity>().HasIndex(cuenta => cuenta.NormalizedEmail).IsUnique();

        // Longitudes explícitas para que las claves compuestas sean iguales al migrar y al probar.
        modelBuilder.Entity<IdentityUserLogin<Guid>>().Property(login => login.LoginProvider).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserLogin<Guid>>().Property(login => login.ProviderKey).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserToken<Guid>>().Property(token => token.LoginProvider).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserToken<Guid>>().Property(token => token.Name).HasMaxLength(128);

        // utf8mb4 permite nombres y biografías con tildes y emojis.
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AwoorldDbContext).Assembly);
    }
}
