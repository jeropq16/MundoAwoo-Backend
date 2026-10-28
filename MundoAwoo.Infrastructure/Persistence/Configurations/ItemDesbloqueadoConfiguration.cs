using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class ItemDesbloqueadoConfiguration : IEntityTypeConfiguration<ItemDesbloqueado>
{
    public void Configure(EntityTypeBuilder<ItemDesbloqueado> builder)
    {
        builder.ToTable("ItemsDesbloqueados");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.FechaDesbloqueo)
            .HasConversion(fecha => fecha.UtcDateTime,
                fecha => new DateTimeOffset(DateTime.SpecifyKind(fecha, DateTimeKind.Utc)))
            .HasColumnType("datetime(6)");

        // Incluso dos solicitudes simultáneas no podrán duplicar un desbloqueo.
        builder.HasIndex(item => new { item.UsuarioId, item.ItemDeAvatarId }).IsUnique();
        builder.HasOne<ItemDeAvatar>().WithMany()
            .HasForeignKey(item => item.ItemDeAvatarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
