using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class PerfilUsuarioConfiguration : IEntityTypeConfiguration<PerfilUsuario>
{
    public void Configure(EntityTypeBuilder<PerfilUsuario> builder)
    {
        builder.ToTable("PerfilesUsuario");
        builder.HasKey(perfil => perfil.Id);
        builder.Property(perfil => perfil.Id).ValueGeneratedNever();
        builder.Property(perfil => perfil.NombreVisible).HasMaxLength(50).IsRequired();
        builder.Property(perfil => perfil.Biografia).HasMaxLength(500).IsRequired();
        // Un usuario puede aparecer como propietario en una sola fila de esta tabla.
        builder.HasIndex(perfil => perfil.UsuarioId).IsUnique();
    }
}
