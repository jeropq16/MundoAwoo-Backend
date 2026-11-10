using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Application.DTOs.Encuentros;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Services;

namespace MundoAwoo.Tests;

public class EncuentroReconexionTests
{
    private sealed class Reloj : TimeProvider
    {
        private DateTimeOffset _ahora = DateTimeOffset.Parse("2026-11-09T12:00:00Z");
        public override DateTimeOffset GetUtcNow() => _ahora;
        public void Avanzar(TimeSpan plazo) => _ahora += plazo;
    }
    private static ParceroDto Parcero() => new(Guid.NewGuid(), "Parcero", new AvatarDto());

    [Fact]
    public void Retorno_ReemplazaConexionVivaSinDuplicarNiPerderChatPoseOFrecuencia()
    {
        var reloj = new Reloj();
        var servicio = new EncuentroService(reloj);
        var parcero = Parcero();
        var sala = servicio.Entrar("vieja", parcero, null);
        var token = servicio.ObtenerTokenReconexion("vieja");
        servicio.Escribir("vieja", "Seguimos aquí");
        servicio.Actuar("vieja", "Bailar");
        var retorno = servicio.Reanudar("nueva", parcero.UsuarioId, sala.Codigo, token);
        Assert.Equal("vieja", retorno.ConexionAnterior);
        var presente = Assert.Single(retorno.Sala.Participantes);
        Assert.True(presente.Conectado);
        Assert.Equal("Bailar", presente.Movimiento);
        Assert.Equal("Seguimos aquí", Assert.Single(retorno.Sala.Mensajes).Texto);
        Assert.Equal(token, servicio.ObtenerTokenReconexion("nueva"));
        Assert.Null(servicio.Desconectar("vieja"));
        Assert.Null(servicio.Salir("vieja"));
        Assert.Throws<ArgumentException>(() => servicio.Escribir("vieja", "Fuera"));
        Assert.Throws<ArgumentException>(() => servicio.Actuar("vieja", "Saludar"));
        Assert.Throws<ArgumentException>(() => servicio.Escribir("nueva", "Sin saltarse el límite"));
        reloj.Avanzar(TimeSpan.FromMilliseconds(750));
        Assert.Equal(2, servicio.Escribir("nueva", "De vuelta").Mensajes.Count);
        var repetido = servicio.Reanudar("nueva", parcero.UsuarioId, sala.Codigo, token);
        Assert.Null(repetido.ConexionAnterior);
        Assert.Single(repetido.Sala.Participantes);
    }

    [Fact]
    public void Desconexion_ReservaUltimoLugarYNoExtiendePlazoConCallbacksRepetidos()
    {
        var reloj = new Reloj();
        var servicio = new EncuentroService(reloj);
        var parcero = Parcero();
        var sala = servicio.Entrar("a", parcero, null);
        var token = servicio.ObtenerTokenReconexion("a");
        Assert.False(Assert.Single(servicio.Desconectar("a")!.Participantes).Conectado);
        reloj.Avanzar(TimeSpan.FromSeconds(119));
        Assert.Null(servicio.Desconectar("a"));
        Assert.Empty(servicio.LimpiarReservas());
        var retorno = servicio.Reanudar("b", parcero.UsuarioId, sala.Codigo, token);
        Assert.True(Assert.Single(retorno.Sala.Participantes).Conectado);
        servicio.Desconectar("b");
        reloj.Avanzar(EncuentroService.PlazoReconexion);
        Assert.Throws<ReconexionNoDisponibleException>(() => servicio.Reanudar("c", parcero.UsuarioId, sala.Codigo, token));
        Assert.Empty(Assert.Single(servicio.LimpiarReservas()).Participantes);
        Assert.Throws<ArgumentException>(() => servicio.Entrar("c", Parcero(), sala.Codigo));
    }

    [Fact]
    public void Retorno_ExigeCuentaYClaveOriginalYLaSalidaLasInvalida()
    {
        var servicio = new EncuentroService();
        var parcero = Parcero();
        var sala = servicio.Entrar("a", parcero, null);
        var token = servicio.ObtenerTokenReconexion("a");
        Assert.Throws<ReconexionNoDisponibleException>(() => servicio.Reanudar("x", Guid.NewGuid(), sala.Codigo, token));
        Assert.Throws<ReconexionNoDisponibleException>(() => servicio.Reanudar("x", parcero.UsuarioId, sala.Codigo, new string('0', 64)));
        Assert.Throws<ReconexionNoDisponibleException>(() => servicio.Reanudar("x", parcero.UsuarioId, "NOEXISTE", token));
        Assert.Single(servicio.Consultar("a").Participantes);
        servicio.Salir("a");
        Assert.Throws<ReconexionNoDisponibleException>(() => servicio.Reanudar("x", parcero.UsuarioId, sala.Codigo, token));
        // Una sala con el mismo usuario que vuelve a entrar tiene una clave distinta.
        var nueva = servicio.Entrar("b", parcero, null);
        Assert.NotEqual(token, servicio.ObtenerTokenReconexion("b"));
        Assert.Throws<ReconexionNoDisponibleException>(() => servicio.Reanudar("x", parcero.UsuarioId, nueva.Codigo, token));
    }

    [Fact]
    public void Reserva_OcupaCupoPeroPermiteVolverAunqueLaSalaEsteLlena()
    {
        var reloj = new Reloj();
        var servicio = new EncuentroService(reloj);
        var parcero = Parcero();
        var sala = servicio.Entrar("a", parcero, null);
        var token = servicio.ObtenerTokenReconexion("a");
        for (var i = 0; i < 5; i++) servicio.Entrar(i.ToString(), Parcero(), sala.Codigo);
        servicio.Desconectar("a");
        Assert.Throws<ArgumentException>(() => servicio.Entrar("extra", Parcero(), sala.Codigo));
        Assert.Equal(6, servicio.Reanudar("b", parcero.UsuarioId, sala.Codigo, token).Sala.Participantes.Count);
        servicio.Desconectar("b");
        reloj.Avanzar(EncuentroService.PlazoReconexion);
        Assert.Equal(5, Assert.Single(servicio.LimpiarReservas()).Participantes.Count);
        Assert.Equal(6, servicio.Entrar("extra", Parcero(), sala.Codigo).Participantes.Count);
    }

    [Fact]
    public async Task RetornosSimultaneos_SoloLaUltimaConexionPuedeParticipar()
    {
        var servicio = new EncuentroService();
        var parcero = Parcero();
        var sala = servicio.Entrar("a", parcero, null);
        var token = servicio.ObtenerTokenReconexion("a");
        var conexiones = Enumerable.Range(0, 20).Select(i => $"retorno-{i}").ToArray();
        await Task.WhenAll(conexiones.Select(c => Task.Run(() => servicio.Reanudar(c, parcero.UsuarioId, sala.Codigo, token))));
        var activas = conexiones.Where(c =>
        {
            try { servicio.Consultar(c); return true; }
            catch (ArgumentException) { return false; }
        }).ToArray();
        var activa = Assert.Single(activas);
        foreach (var obsoleta in conexiones.Where(c => c != activa)) Assert.Null(servicio.Desconectar(obsoleta));
        Assert.True(Assert.Single(servicio.Consultar(activa).Participantes).Conectado);
    }

    [Fact]
    public void CallbackDuplicado_NoRenuevaReservaVencida()
    {
        var reloj = new Reloj();
        var servicio = new EncuentroService(reloj);
        servicio.Entrar("a", Parcero(), null);
        servicio.Desconectar("a");
        reloj.Avanzar(TimeSpan.FromSeconds(119));
        Assert.Null(servicio.Desconectar("a"));
        reloj.Avanzar(TimeSpan.FromSeconds(1));
        Assert.Empty(Assert.Single(servicio.LimpiarReservas()).Participantes);
    }
}
