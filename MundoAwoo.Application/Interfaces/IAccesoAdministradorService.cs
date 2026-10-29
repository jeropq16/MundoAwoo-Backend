namespace MundoAwoo.Application.Interfaces;

public interface IAccesoAdministradorService
{
    Task<bool> EsAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task ExigirAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
