using MundoAwoo.Application.Services;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Tests.Services;

public class UsuarioServiceTests
{
    private readonly UsuarioService _usuarios = new();
    private readonly CatalogoService _catalogo = new();

    [Fact]
    public void CrearUsuario_CreaSuPerfilYUnInventarioVacio()
    {
        var usuario = _usuarios.Crear("  Awoo Fan  ");

        Assert.NotEqual(Guid.Empty, usuario.Id);
        Assert.NotEqual(Guid.Empty, usuario.Perfil.Id);
        Assert.Equal(usuario.Id, usuario.Perfil.UsuarioId);
        Assert.Equal("Awoo Fan", usuario.Perfil.NombreVisible);
        Assert.Empty(usuario.Perfil.Biografia);
        Assert.Empty(usuario.ItemsDesbloqueados);
        Assert.Equal(TimeSpan.Zero, usuario.FechaRegistro.Offset);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CrearUsuario_SinNombre_Falla(string? nombre)
    {
        Assert.Throws<ArgumentException>(() => _usuarios.Crear(nombre!));
    }

    [Fact]
    public void ActualizarPerfil_ConBiografiaInvalida_NoCambiaNingunaPropiedad()
    {
        var usuario = _usuarios.Crear("Nombre original");
        _usuarios.ActualizarPerfil(usuario, "Nombre original", "Biografía original");

        Assert.Throws<ArgumentException>(() => _usuarios.ActualizarPerfil(usuario, "Nuevo nombre", new string('a', 501)));

        Assert.Equal("Nombre original", usuario.Perfil.NombreVisible);
        Assert.Equal("Biografía original", usuario.Perfil.Biografia);
    }

    [Fact]
    public void ActualizarPerfil_AceptaLosLimitesDeLongitud()
    {
        var usuario = _usuarios.Crear("Fan");

        _usuarios.ActualizarPerfil(usuario, new string('n', 50), new string('b', 500));

        Assert.Equal(50, usuario.Perfil.NombreVisible.Length);
        Assert.Equal(500, usuario.Perfil.Biografia.Length);
        Assert.Throws<ArgumentException>(() => _usuarios.ActualizarPerfil(usuario, new string('n', 51), ""));
    }

    [Fact]
    public void DesbloquearItem_DosVeces_ConservaUnSoloRegistro()
    {
        var usuario = _usuarios.Crear("Fan");
        var item = _catalogo.CrearItem("Gorra", _catalogo.CrearCategoria("Accesorios", TipoCategoria.Accesorio),
            RanuraAvatar.Cabeza, "gorra.png");
        _usuarios.DesbloquearItem(usuario, item);
        Guid desbloqueoId = Assert.Single(usuario.ItemsDesbloqueados).Id;

        _usuarios.DesbloquearItem(usuario, item);

        var desbloqueado = Assert.Single(usuario.ItemsDesbloqueados);
        Assert.Equal(desbloqueoId, desbloqueado.Id);
        Assert.Equal(usuario.Id, desbloqueado.UsuarioId);
        Assert.Equal(item.Id, desbloqueado.ItemDeAvatarId);
        Assert.True(_usuarios.TieneItemDesbloqueado(usuario, item.Id));
        Assert.False(_usuarios.TieneItemDesbloqueado(usuario, Guid.NewGuid()));
    }
}
