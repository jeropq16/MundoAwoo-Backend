using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class ItemDeAvatarConfiguration : IEntityTypeConfiguration<ItemDeAvatar>
{
    public void Configure(EntityTypeBuilder<ItemDeAvatar> builder)
    {
        builder.ToTable("ItemsDeAvatar");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(item => item.RecursoVisual).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Ranura).HasConversion<int>();

        // No borrar una categoría mientras tenga ítems asociados.
        builder.HasOne<Categoria>().WithMany()
            .HasForeignKey(item => item.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
