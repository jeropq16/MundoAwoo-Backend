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
        // El dominio genera el Guid. MySQL no debe generar un segundo identificador.
        builder.Property(usuario => usuario.Id).ValueGeneratedNever();

        // MySQL datetime no guarda el offset: convertimos explícitamente a UTC al escribir y leer.
        builder.Property(usuario => usuario.FechaRegistro)
            .HasConversion(fecha => fecha.UtcDateTime,
                fecha => new DateTimeOffset(DateTime.SpecifyKind(fecha, DateTimeKind.Utc)))
            .HasColumnType("datetime(6)");

        builder.HasOne(usuario => usuario.Perfil)
            .WithOne()
            .HasForeignKey<PerfilUsuario>(perfil => perfil.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF escribe en la lista privada, no en la vista de solo lectura del dominio.
        builder.HasMany(usuario => usuario.ItemsDesbloqueados)
            .WithOne()
            .HasForeignKey(item => item.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(usuario => usuario.ItemsDesbloqueados)
            .HasField("_itemsDesbloqueados")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
