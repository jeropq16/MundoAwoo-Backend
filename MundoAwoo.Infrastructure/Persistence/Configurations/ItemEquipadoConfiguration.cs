using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class ItemEquipadoConfiguration : IEntityTypeConfiguration<ItemEquipado>
{
    public void Configure(EntityTypeBuilder<ItemEquipado> builder)
    {
        builder.ToTable("ItemsEquipados");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Ranura).HasConversion<int>();

        // Un avatar tiene como máximo un ítem en cada ranura y no repite un mismo ítem.
        builder.HasIndex(item => new { item.AvatarId, item.Ranura }).IsUnique();
        builder.HasIndex(item => new { item.AvatarId, item.ItemDeAvatarId }).IsUnique();
        builder.HasOne<ItemDeAvatar>().WithMany()
            .HasForeignKey(item => item.ItemDeAvatarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
