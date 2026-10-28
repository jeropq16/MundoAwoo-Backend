using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Tests.Domain;

public class UsuarioTests
{
    [Fact]
    public void CrearUsuario_CreaSuPerfilYUnInventarioVacio()
    {
        var usuario = new Usuario("  Awoo Fan  ");

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
        Assert.Throws<ArgumentException>(() => new Usuario(nombre!));
    }

    [Fact]
    public void ActualizarPerfil_ConBiografiaInvalida_NoCambiaNingunaPropiedad()
    {
        var usuario = new Usuario("Nombre original");
        usuario.ActualizarPerfil("Nombre original", "Biografía original");

        Assert.Throws<ArgumentException>(() => usuario.ActualizarPerfil("Nuevo nombre", new string('a', 501)));

        Assert.Equal("Nombre original", usuario.Perfil.NombreVisible);
        Assert.Equal("Biografía original", usuario.Perfil.Biografia);
    }

    [Fact]
    public void ActualizarPerfil_AceptaLosLimitesDeLongitud()
    {
        var usuario = new Usuario("Fan");

        usuario.ActualizarPerfil(new string('n', 50), new string('b', 500));

        Assert.Equal(50, usuario.Perfil.NombreVisible.Length);
        Assert.Equal(500, usuario.Perfil.Biografia.Length);
        Assert.Throws<ArgumentException>(() => usuario.ActualizarPerfil(new string('n', 51), ""));
    }

    [Fact]
    public void DesbloquearItem_DosVeces_ConservaUnSoloRegistro()
    {
        var usuario = new Usuario("Fan");
        var item = new ItemDeAvatar("Gorra", new Categoria("Accesorios", TipoCategoria.Accesorio),
            RanuraAvatar.Cabeza, "gorra.png");
        usuario.DesbloquearItem(item);
        Guid desbloqueoId = Assert.Single(usuario.ItemsDesbloqueados).Id;

        usuario.DesbloquearItem(item);

        var desbloqueado = Assert.Single(usuario.ItemsDesbloqueados);
        Assert.Equal(desbloqueoId, desbloqueado.Id);
        Assert.Equal(usuario.Id, desbloqueado.UsuarioId);
        Assert.Equal(item.Id, desbloqueado.ItemDeAvatarId);
        Assert.True(usuario.TieneItemDesbloqueado(item.Id));
        Assert.False(usuario.TieneItemDesbloqueado(Guid.NewGuid()));
    }
}
