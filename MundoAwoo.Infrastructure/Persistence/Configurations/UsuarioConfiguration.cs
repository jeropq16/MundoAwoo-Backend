using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(usuario => usuario.Id);
        // Application genera el Guid. MySQL no debe generar otro identificador.
        builder.Property(usuario => usuario.Id).ValueGeneratedNever();
        builder.Property(usuario => usuario.Correo).HasMaxLength(256);
        builder.HasIndex(usuario => usuario.Correo).IsUnique();
        builder.Property(usuario => usuario.PasswordHash).HasMaxLength(255);
        builder.Property(usuario => usuario.IntentosFallidos).HasDefaultValue(0);
        builder.Property(usuario => usuario.EsAdministrador).HasDefaultValue(false);
        builder.Property(usuario => usuario.BloqueadoHastaUtc).HasColumnType("datetime(6)");

        // MySQL datetime no guarda el offset: convertimos explícitamente a UTC al escribir y leer.
        builder.Property(usuario => usuario.FechaRegistro)
            .HasConversion(fecha => fecha.UtcDateTime,
                fecha => new DateTimeOffset(DateTime.SpecifyKind(fecha, DateTimeKind.Utc)))
            .HasColumnType("datetime(6)");

        builder.HasOne(usuario => usuario.Perfil)
            .WithOne()
            .HasForeignKey<PerfilUsuario>(perfil => perfil.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(usuario => usuario.ItemsDesbloqueados)
            .WithOne()
            .HasForeignKey(item => item.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
