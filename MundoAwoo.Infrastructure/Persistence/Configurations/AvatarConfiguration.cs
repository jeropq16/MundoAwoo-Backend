using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class AvatarConfiguration : IEntityTypeConfiguration<Avatar>
{
    public void Configure(EntityTypeBuilder<Avatar> builder)
    {
        builder.ToTable("Avatares");
        builder.HasKey(avatar => avatar.Id);
        builder.Property(avatar => avatar.Id).ValueGeneratedNever();

        builder.Property(avatar => avatar.Personaje3D)
            .HasMaxLength(16).HasDefaultValue("ghetto").IsRequired();
        builder.Property(avatar => avatar.ChaquetaGhetto).HasDefaultValue(false);
        builder.Property(avatar => avatar.ChaquetaQueen).HasDefaultValue(false);

        // El objeto ColorHex se guarda en una columna; no necesita una tabla propia.
        builder.Property(avatar => avatar.ColorFondo)
            .HasConversion(color => color.Valor, valor => new ColorHex(valor))
            .HasColumnType("char(7)")
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithOne()
            .HasForeignKey<Avatar>(avatar => avatar.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(avatar => avatar.UsuarioId).IsUnique();

        builder.HasMany(avatar => avatar.ItemsEquipados)
            .WithOne()
            .HasForeignKey(item => item.AvatarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
