using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class SesionRefreshTokenConfiguration : IEntityTypeConfiguration<SesionRefreshToken>
{
    public void Configure(EntityTypeBuilder<SesionRefreshToken> builder)
    {
        builder.ToTable("SesionesRefreshToken");
        builder.HasKey(sesion => sesion.Id);
        builder.Property(sesion => sesion.Id).ValueGeneratedNever();
        builder.Property(sesion => sesion.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(sesion => sesion.ExpiraEnUtc).HasColumnType("datetime(6)");
        builder.Property(sesion => sesion.RevocadoEnUtc).HasColumnType("datetime(6)");
        builder.HasIndex(sesion => sesion.UsuarioId);
        builder.HasOne<Usuario>().WithMany()
            .HasForeignKey(sesion => sesion.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
