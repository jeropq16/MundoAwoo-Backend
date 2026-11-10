using Microsoft.AspNetCore.SignalR;
using MundoAwoo.Application.Services;

namespace MundoAwoo.Api.Hubs;

public sealed class ReservasEncuentroWorker(EncuentroService salas, IHubContext<EncuentroHub> hub) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            foreach (var sala in salas.LimpiarReservas())
                await hub.Clients.Group(sala.Codigo).SendAsync("SalaActualizada", sala, stoppingToken);
    }
}
