using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Application.DTOs.Encuentros;
using MundoAwoo.Application.Services;

namespace MundoAwoo.Tests;

public class EncuentroServiceTests
{
    private static ParceroDto Parcero() => new(Guid.NewGuid(), "Parcero", new AvatarDto());
    [Fact]
    public void Sala_AislaMensajesYMovimientosYUsaIdentidadDelServidor()
    {
        var servicio = new EncuentroService();
        var primero = Parcero();
        var a = servicio.Entrar("a", primero, null);
        servicio.Entrar("b", Parcero(), a.Codigo.ToLowerInvariant());
        var otra = servicio.Entrar("c", Parcero(), null);
        var chat = servicio.Escribir("a", "  Hola parceros  ");
        Assert.Equal(primero.UsuarioId, Assert.Single(chat.Mensajes).UsuarioId);
        Assert.Equal("Hola parceros", chat.Mensajes[0].Texto);
        Assert.Equal(2, chat.Participantes.Count);
        Assert.Empty(otra.Mensajes);
        var baile = servicio.Actuar("b", "Bailar");
        Assert.Equal("Reposo", baile.Participantes[0].Movimiento);
        Assert.Equal("Bailar", baile.Participantes[1].Movimiento);
        Assert.True(baile.Revision > chat.Revision);
        Assert.Empty(servicio.Actuar("c", "Saludar").Mensajes);
    }

    [Fact]
    public void Sala_ValidaPertenenciaContenidoYFrecuencia()
    {
        var servicio = new EncuentroService();
        Assert.Throws<ArgumentException>(() => servicio.Escribir("ajeno", "Hola"));
        Assert.Throws<ArgumentException>(() => servicio.Actuar("ajeno", "Bailar"));
        Assert.Throws<ArgumentException>(() => servicio.Entrar("a", Parcero(), "NOEXISTE"));
        servicio.Entrar("a", Parcero(), null);
        Assert.Throws<ArgumentException>(() => servicio.Escribir("a", " "));
        Assert.Throws<ArgumentException>(() => servicio.Escribir("a", new string('x', 501)));
        Assert.Throws<ArgumentException>(() => servicio.Actuar("a", "NoValido"));
        servicio.Escribir("a", "Primero");
        Assert.Throws<ArgumentException>(() => servicio.Escribir("a", "Demasiado pronto"));
    }

    [Fact]
    public void Sala_ImpideDuplicadosYDesapareceAlSalirTodos()
    {
        var servicio = new EncuentroService();
        var parcero = Parcero();
        var sala = servicio.Entrar("a", parcero, null);
        Assert.Throws<ArgumentException>(() => servicio.Entrar("a", parcero, null));
        Assert.Throws<ArgumentException>(() => servicio.Entrar("b", parcero, sala.Codigo));
        servicio.Salir("a");
        Assert.Null(servicio.Salir("a"));
        Assert.Throws<ArgumentException>(() => servicio.Entrar("b", Parcero(), sala.Codigo));
        Assert.Throws<ArgumentException>(() => servicio.Escribir("a", "Fuera"));
    }

    [Fact]
    public async Task Sala_NoSobrepasaCapacidadConEntradasSimultaneas()
    {
        var servicio = new EncuentroService();
        var sala = servicio.Entrar("anfitrion", Parcero(), null);
        var intentos = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            try { servicio.Entrar(i.ToString(), Parcero(), sala.Codigo); return true; }
            catch (ArgumentException) { return false; }
        }));
        Assert.Equal(5, (await Task.WhenAll(intentos)).Count(entro => entro));
        Assert.Equal(6, servicio.Actuar("anfitrion", "Reposo").Participantes.Count);
    }
}
