using MundoAwoo.Application.Services;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Tests.Services;

public class CatalogoServiceTests
{
    private readonly CatalogoService _catalogo = new();

    [Theory]
    [InlineData(TipoCategoria.Ropa, RanuraAvatar.Torso)]
    [InlineData(TipoCategoria.Ropa, RanuraAvatar.Piernas)]
    [InlineData(TipoCategoria.Ropa, RanuraAvatar.Pies)]
    [InlineData(TipoCategoria.Accesorio, RanuraAvatar.Cabeza)]
    [InlineData(TipoCategoria.Accesorio, RanuraAvatar.Ojos)]
    [InlineData(TipoCategoria.Accesorio, RanuraAvatar.Cuello)]
    [InlineData(TipoCategoria.Baile, RanuraAvatar.Animacion)]
    [InlineData(TipoCategoria.Gesto, RanuraAvatar.Animacion)]
    public void CrearItem_ConRanuraCompatible_ConservaLaCategoria(TipoCategoria tipo, RanuraAvatar ranura)
    {
        var categoria = _catalogo.CrearCategoria("Categoría", tipo);
        var item = _catalogo.CrearItem(" Ítem ", categoria, ranura, " recurso.png ");

        Assert.Equal(categoria.Id, item.CategoriaId);
        Assert.Equal(ranura, item.Ranura);
        Assert.Equal("Ítem", item.Nombre);
        Assert.Equal("recurso.png", item.RecursoVisual);
    }

    [Theory]
    [InlineData(TipoCategoria.Ropa, RanuraAvatar.Animacion)]
    [InlineData(TipoCategoria.Accesorio, RanuraAvatar.Torso)]
    [InlineData(TipoCategoria.Baile, RanuraAvatar.Cabeza)]
    [InlineData(TipoCategoria.Gesto, RanuraAvatar.Pies)]
    public void CrearItem_ConRanuraIncompatible_Falla(TipoCategoria tipo, RanuraAvatar ranura)
    {
        var categoria = _catalogo.CrearCategoria("Categoría", tipo);

        Assert.Throws<ArgumentException>(() => _catalogo.CrearItem("Ítem", categoria, ranura, "recurso.png"));
    }

    [Fact]
    public void CrearCatalogo_ConEnumsInexistentes_Falla()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _catalogo.CrearCategoria("Inválida", (TipoCategoria)999));
        var categoria = _catalogo.CrearCategoria("Ropa", TipoCategoria.Ropa);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _catalogo.CrearItem("Ítem", categoria, (RanuraAvatar)999, "recurso.png"));
    }
}
