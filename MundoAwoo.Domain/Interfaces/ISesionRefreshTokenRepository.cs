using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Domain.Interfaces;

public interface ISesionRefreshTokenRepository
{
    void Agregar(SesionRefreshToken sesion);
    Task<SesionRefreshToken?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RotarAsync(Guid id, string hashAnterior, string hashNuevo, DateTime ahoraUtc,
        CancellationToken cancellationToken = default);
    Task<bool> RevocarAsync(Guid id, string hashActual, DateTime ahoraUtc,
        CancellationToken cancellationToken = default);
}
