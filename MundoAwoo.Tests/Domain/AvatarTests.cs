using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Tests.Domain;

public class AvatarTests
{
    [Fact]
    public void Equipar_ItemBloqueado_NoModificaElAvatar()
    {
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);
        var item = CrearCamiseta("Camiseta del álbum");

        Assert.Throws<InvalidOperationException>(() => avatar.Equipar(item, usuario));
        Assert.Empty(avatar.ItemsEquipados);
    }

    [Fact]
    public void Equipar_ConInventarioDeOtroUsuario_NoPermiteUsarSuDesbloqueo()
    {
        var propietario = new Usuario("Propietario");
        var otroUsuario = new Usuario("Otro fan");
        var avatar = new Avatar(propietario.Id);
        var item = CrearCamiseta("Camiseta");
        otroUsuario.DesbloquearItem(item);

        Assert.Throws<InvalidOperationException>(() => avatar.Equipar(item, otroUsuario));
        Assert.Empty(avatar.ItemsEquipados);
    }

    [Fact]
    public void Equipar_DosPrendasEnLaMismaRanura_ConservaLaPrimera()
    {
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);
        var primera = CrearCamiseta("Primera");
        var segunda = CrearCamiseta("Segunda");
        usuario.DesbloquearItem(primera);
        usuario.DesbloquearItem(segunda);
        avatar.Equipar(primera, usuario);

        Assert.Throws<InvalidOperationException>(() => avatar.Equipar(segunda, usuario));

        // Esta comprobación también detecta que no se borre la prenda anterior al fallar.
        Assert.Equal(primera.Id, Assert.Single(avatar.ItemsEquipados).ItemDeAvatarId);
    }

    [Fact]
    public void Equipar_MismoItemDosVeces_NoDuplicaLaSeleccion()
    {
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);
        var item = CrearCamiseta("Camiseta");
        usuario.DesbloquearItem(item);

        avatar.Equipar(item, usuario);
        avatar.Equipar(item, usuario);

        var equipado = Assert.Single(avatar.ItemsEquipados);
        Assert.Equal(avatar.Id, equipado.AvatarId);
        Assert.Equal(item.Id, equipado.ItemDeAvatarId);
        Assert.Equal(RanuraAvatar.Torso, equipado.Ranura);
    }

    [Fact]
    public void Desequipar_LiberaLaRanuraYConservaElDesbloqueo()
    {
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);
        var primera = CrearCamiseta("Primera");
        var segunda = CrearCamiseta("Segunda");
        usuario.DesbloquearItem(primera);
        usuario.DesbloquearItem(segunda);
        avatar.Equipar(primera, usuario);

        avatar.Desequipar(primera.Id);
        avatar.Desequipar(primera.Id);
        avatar.Equipar(segunda, usuario);

        Assert.Equal(segunda.Id, Assert.Single(avatar.ItemsEquipados).ItemDeAvatarId);
        Assert.True(usuario.TieneItemDesbloqueado(primera.Id));
        Assert.Equal(2, usuario.ItemsDesbloqueados.Count);
    }

    [Fact]
    public void Equipar_RopaYAccesorio_PuedenConvivir()
    {
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);
        var camiseta = CrearCamiseta("Camiseta");
        var lentes = new ItemDeAvatar("Lentes", new Categoria("Accesorios", TipoCategoria.Accesorio),
            RanuraAvatar.Ojos, "lentes.png");
        usuario.DesbloquearItem(camiseta);
        usuario.DesbloquearItem(lentes);

        avatar.Equipar(camiseta, usuario);
        avatar.Equipar(lentes, usuario);

        Assert.Equal(2, avatar.ItemsEquipados.Count);
    }

    [Fact]
    public void Equipar_BaileYGesto_CompartenLaRanuraDeAnimacion()
    {
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);
        var baile = new ItemDeAvatar("Baile", new Categoria("Bailes", TipoCategoria.Baile),
            RanuraAvatar.Animacion, "baile.json");
        var gesto = new ItemDeAvatar("Saludo", new Categoria("Gestos", TipoCategoria.Gesto),
            RanuraAvatar.Animacion, "saludo.json");
        usuario.DesbloquearItem(baile);
        usuario.DesbloquearItem(gesto);
        avatar.Equipar(baile, usuario);

        Assert.Throws<InvalidOperationException>(() => avatar.Equipar(gesto, usuario));
        Assert.Equal(baile.Id, Assert.Single(avatar.ItemsEquipados).ItemDeAvatarId);
    }

    [Fact]
    public void CambiarColorFondo_NoModificaElObjetoDeValorAnterior()
    {
        var avatar = new Avatar(Guid.NewGuid());
        ColorHex anterior = avatar.ColorFondo;

        avatar.CambiarColorFondo(new ColorHex("#aabbcc"));

        Assert.Equal("#FFFFFF", anterior.Valor);
        Assert.Equal("#AABBCC", avatar.ColorFondo.Valor);
    }

    [Fact]
    public void CrearAvatar_SinUsuario_Falla()
    {
        Assert.Throws<ArgumentException>(() => new Avatar(Guid.Empty));
    }

    [Fact]
    public void ColeccionesPublicas_NoPermitenVaciarLosAgregados()
    {
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);
        var item = CrearCamiseta("Camiseta");
        usuario.DesbloquearItem(item);
        avatar.Equipar(item, usuario);

        Assert.Throws<NotSupportedException>(() => ((ICollection<ItemEquipado>)avatar.ItemsEquipados).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<ItemDesbloqueado>)usuario.ItemsDesbloqueados).Clear());
        Assert.Single(avatar.ItemsEquipados);
        Assert.Single(usuario.ItemsDesbloqueados);
    }

    private static ItemDeAvatar CrearCamiseta(string nombre)
    {
        return new ItemDeAvatar(nombre, new Categoria("Ropa", TipoCategoria.Ropa),
            RanuraAvatar.Torso, "camiseta.png");
    }
}
