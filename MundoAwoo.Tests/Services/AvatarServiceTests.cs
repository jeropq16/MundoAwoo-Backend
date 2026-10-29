using MundoAwoo.Application.Services;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Tests.Services;

public class AvatarServiceTests
{
    private readonly UsuarioService _usuarios = new();
    private readonly CatalogoService _catalogo = new();
    private readonly AvatarService _avatares;

    public AvatarServiceTests() => _avatares = new AvatarService(_usuarios);

    [Fact]
    public void Equipar_ItemBloqueado_NoModificaElAvatar()
    {
        var usuario = _usuarios.Crear("Fan");
        var avatar = _avatares.Crear(usuario.Id);
        var item = CrearCamiseta("Camiseta del álbum");

        Assert.Throws<InvalidOperationException>(() => _avatares.Equipar(avatar, item, usuario));
        Assert.Empty(avatar.ItemsEquipados);
    }

    [Fact]
    public void Equipar_ConInventarioDeOtroUsuario_NoPermiteUsarSuDesbloqueo()
    {
        var propietario = _usuarios.Crear("Propietario");
        var otroUsuario = _usuarios.Crear("Otro fan");
        var avatar = _avatares.Crear(propietario.Id);
        var item = CrearCamiseta("Camiseta");
        _usuarios.DesbloquearItem(otroUsuario, item);

        Assert.Throws<InvalidOperationException>(() => _avatares.Equipar(avatar, item, otroUsuario));
        Assert.Empty(avatar.ItemsEquipados);
    }

    [Fact]
    public void Equipar_DosPrendasEnLaMismaRanura_ConservaLaPrimera()
    {
        var usuario = _usuarios.Crear("Fan");
        var avatar = _avatares.Crear(usuario.Id);
        var primera = CrearCamiseta("Primera");
        var segunda = CrearCamiseta("Segunda");
        _usuarios.DesbloquearItem(usuario, primera);
        _usuarios.DesbloquearItem(usuario, segunda);
        _avatares.Equipar(avatar, primera, usuario);

        Assert.Throws<InvalidOperationException>(() => _avatares.Equipar(avatar, segunda, usuario));

        // Esta comprobación también detecta que no se borre la prenda anterior al fallar.
        Assert.Equal(primera.Id, Assert.Single(avatar.ItemsEquipados).ItemDeAvatarId);
    }

    [Fact]
    public void Equipar_MismoItemDosVeces_NoDuplicaLaSeleccion()
    {
        var usuario = _usuarios.Crear("Fan");
        var avatar = _avatares.Crear(usuario.Id);
        var item = CrearCamiseta("Camiseta");
        _usuarios.DesbloquearItem(usuario, item);

        _avatares.Equipar(avatar, item, usuario);
        _avatares.Equipar(avatar, item, usuario);

        var equipado = Assert.Single(avatar.ItemsEquipados);
        Assert.Equal(avatar.Id, equipado.AvatarId);
        Assert.Equal(item.Id, equipado.ItemDeAvatarId);
        Assert.Equal(RanuraAvatar.Torso, equipado.Ranura);
    }

    [Fact]
    public void Desequipar_LiberaLaRanuraYConservaElDesbloqueo()
    {
        var usuario = _usuarios.Crear("Fan");
        var avatar = _avatares.Crear(usuario.Id);
        var primera = CrearCamiseta("Primera");
        var segunda = CrearCamiseta("Segunda");
        _usuarios.DesbloquearItem(usuario, primera);
        _usuarios.DesbloquearItem(usuario, segunda);
        _avatares.Equipar(avatar, primera, usuario);

        _avatares.Desequipar(avatar, primera.Id);
        _avatares.Desequipar(avatar, primera.Id);
        _avatares.Equipar(avatar, segunda, usuario);

        Assert.Equal(segunda.Id, Assert.Single(avatar.ItemsEquipados).ItemDeAvatarId);
        Assert.True(_usuarios.TieneItemDesbloqueado(usuario, primera.Id));
        Assert.Equal(2, usuario.ItemsDesbloqueados.Count);
    }

    [Fact]
    public void Equipar_RopaYAccesorio_PuedenConvivir()
    {
        var usuario = _usuarios.Crear("Fan");
        var avatar = _avatares.Crear(usuario.Id);
        var camiseta = CrearCamiseta("Camiseta");
        var lentes = _catalogo.CrearItem("Lentes", _catalogo.CrearCategoria("Accesorios", TipoCategoria.Accesorio),
            RanuraAvatar.Ojos, "lentes.png");
        _usuarios.DesbloquearItem(usuario, camiseta);
        _usuarios.DesbloquearItem(usuario, lentes);

        _avatares.Equipar(avatar, camiseta, usuario);
        _avatares.Equipar(avatar, lentes, usuario);

        Assert.Equal(2, avatar.ItemsEquipados.Count);
    }

    [Fact]
    public void Equipar_BaileYGesto_CompartenLaRanuraDeAnimacion()
    {
        var usuario = _usuarios.Crear("Fan");
        var avatar = _avatares.Crear(usuario.Id);
        var baile = _catalogo.CrearItem("Baile", _catalogo.CrearCategoria("Bailes", TipoCategoria.Baile),
            RanuraAvatar.Animacion, "baile.json");
        var gesto = _catalogo.CrearItem("Saludo", _catalogo.CrearCategoria("Gestos", TipoCategoria.Gesto),
            RanuraAvatar.Animacion, "saludo.json");
        _usuarios.DesbloquearItem(usuario, baile);
        _usuarios.DesbloquearItem(usuario, gesto);
        _avatares.Equipar(avatar, baile, usuario);

        Assert.Throws<InvalidOperationException>(() => _avatares.Equipar(avatar, gesto, usuario));
        Assert.Equal(baile.Id, Assert.Single(avatar.ItemsEquipados).ItemDeAvatarId);
    }

    [Fact]
    public void CambiarColorFondo_NoModificaElObjetoDeValorAnterior()
    {
        var avatar = _avatares.Crear(Guid.NewGuid());
        ColorHex anterior = avatar.ColorFondo;

        _avatares.CambiarColorFondo(avatar, new ColorHex("#aabbcc"));

        Assert.Equal("#FFFFFF", anterior.Valor);
        Assert.Equal("#AABBCC", avatar.ColorFondo.Valor);
    }

    [Fact]
    public void CrearAvatar_SinUsuario_Falla()
    {
        Assert.Throws<ArgumentException>(() => _avatares.Crear(Guid.Empty));
    }

    private ItemDeAvatar CrearCamiseta(string nombre)
    {
        return _catalogo.CrearItem(nombre, _catalogo.CrearCategoria("Ropa", TipoCategoria.Ropa),
            RanuraAvatar.Torso, "camiseta.png");
    }
}
