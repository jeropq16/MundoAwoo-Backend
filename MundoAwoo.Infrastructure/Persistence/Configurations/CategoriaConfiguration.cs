using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Infrastructure.Persistence.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");
        builder.HasKey(categoria => categoria.Id);
        builder.Property(categoria => categoria.Id).ValueGeneratedNever();
        builder.Property(categoria => categoria.Nombre).HasMaxLength(50).IsRequired();
        // Guardamos el enum como número. No cambiaremos los valores numéricos ya publicados.
        builder.Property(categoria => categoria.Tipo).HasConversion<int>();
    }
}
