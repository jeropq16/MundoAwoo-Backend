using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.DTOs.Encuentros;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Services;

namespace MundoAwoo.Api.Hubs;

[Authorize(Policy = AutenticacionJwt.PoliticaPerfilPropio)]
public class EncuentroHub(EncuentroService salas, IPerfilService perfiles, IAvatarAplicacionService avatares) : Hub
{
    public Task<EntradaSalaDto> CrearSala() => Entrar(null);
    public Task<EntradaSalaDto> UnirseSala(string codigo) => Entrar(codigo ?? "");

    private async Task<EntradaSalaDto> Entrar(string? codigo)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(Context.User!, out var usuarioId))
            throw new HubException("Inicia sesión de nuevo para entrar.");
        // La identidad y el avatar siempre proceden del servidor, no del cliente.
        var perfil = await perfiles.ObtenerAsync(usuarioId, Context.ConnectionAborted);
        var avatar = await avatares.ObtenerAsync(usuarioId, Context.ConnectionAborted);
        var sala = Validar(() => salas.Entrar(Context.ConnectionId, new(usuarioId, perfil.NombreVisible, avatar), codigo));
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sala.Codigo, Context.ConnectionAborted);
            sala = salas.Consultar(Context.ConnectionId);
            await Publicar(sala);
            return new(sala, salas.ObtenerTokenReconexion(Context.ConnectionId));
        }
        catch
        {
            var salida = salas.Salir(Context.ConnectionId);
            if (salida is not null) await Publicar(salida);
            throw;
        }
    }

    public async Task<EntradaSalaDto> ReanudarSala(string codigo, string tokenReconexion)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(Context.User!, out var usuarioId))
            throw new HubException("Inicia sesión de nuevo para entrar.");
        var retorno = Validar(() => salas.Reanudar(Context.ConnectionId, usuarioId, codigo, tokenReconexion));
        try
        {
            if (retorno.ConexionAnterior is not null)
                await Groups.RemoveFromGroupAsync(retorno.ConexionAnterior, retorno.Sala.Codigo);
            await Groups.AddToGroupAsync(Context.ConnectionId, retorno.Sala.Codigo, Context.ConnectionAborted);
            var sala = salas.Consultar(Context.ConnectionId);
            await Publicar(sala);
            return new(sala, salas.ObtenerTokenReconexion(Context.ConnectionId));
        }
        catch
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, retorno.Sala.Codigo);
            var sala = salas.Desconectar(Context.ConnectionId);
            if (sala is not null) await Publicar(sala);
            throw;
        }
    }

    public async Task SalirSala()
    {
        var sala = salas.Salir(Context.ConnectionId);
        if (sala is null) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala.Codigo);
        await Publicar(sala);
    }

    public Task Escribir(string texto) => Publicar(Validar(() => salas.Escribir(Context.ConnectionId, texto)));
    public Task Actuar(string movimiento) => Publicar(Validar(() => salas.Actuar(Context.ConnectionId, movimiento)));

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sala = salas.Desconectar(Context.ConnectionId);
        if (sala is not null) await Publicar(sala);
        await base.OnDisconnectedAsync(exception);
    }
    private Task Publicar(SalaDto sala) => Clients.Group(sala.Codigo).SendAsync("SalaActualizada", sala);
    private static T Validar<T>(Func<T> accion)
    {
        try { return accion(); }
        catch (ReconexionNoDisponibleException) { throw new HubException("REANUDACION_NO_DISPONIBLE"); }
        catch (ArgumentException error) { throw new HubException(error.Message); }
    }
}
