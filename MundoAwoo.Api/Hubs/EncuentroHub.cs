using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.DTOs.Encuentros;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Application.Services;

namespace MundoAwoo.Api.Hubs;

[Authorize(Policy = AutenticacionJwt.PoliticaPerfilPropio)]
public class EncuentroHub(EncuentroService salas, IPerfilService perfiles, IAvatarAplicacionService avatares) : Hub
{
    public Task<SalaDto> CrearSala() => Entrar(null);
    public Task<SalaDto> UnirseSala(string codigo) => Entrar(codigo ?? "");

    private async Task<SalaDto> Entrar(string? codigo)
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
            return sala;
        }
        catch
        {
            var salida = salas.Salir(Context.ConnectionId);
            if (salida is not null) await Publicar(salida);
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
        var sala = salas.Salir(Context.ConnectionId);
        if (sala is not null) await Publicar(sala);
        await base.OnDisconnectedAsync(exception);
    }
    private Task Publicar(SalaDto sala) => Clients.Group(sala.Codigo).SendAsync("SalaActualizada", sala);
    private static T Validar<T>(Func<T> accion)
    {
        try { return accion(); }
        catch (ArgumentException error) { throw new HubException(error.Message); }
    }
}
