using Microsoft.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Infrastructure.Persistence.Repositories;

public class SesionRefreshTokenRepository : ISesionRefreshTokenRepository
{
    private readonly AwoorldDbContext _contexto;

    public SesionRefreshTokenRepository(AwoorldDbContext contexto) => _contexto = contexto;

    public void Agregar(SesionRefreshToken sesion) => _contexto.SesionesRefreshToken.Add(sesion);

    public Task<SesionRefreshToken?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _contexto.SesionesRefreshToken.AsNoTracking()
            .SingleOrDefaultAsync(sesion => sesion.Id == id, cancellationToken);

    public async Task<bool> RotarAsync(Guid id, string hashAnterior, string hashNuevo, DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        int modificadas = await _contexto.SesionesRefreshToken
            .Where(sesion => sesion.Id == id && sesion.TokenHash == hashAnterior &&
                sesion.RevocadoEnUtc == null && sesion.ExpiraEnUtc > ahoraUtc)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(sesion => sesion.TokenHash, hashNuevo),
                cancellationToken);
        return modificadas == 1;
    }

    public async Task<bool> RevocarAsync(Guid id, string hashActual, DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        int modificadas = await _contexto.SesionesRefreshToken
            .Where(sesion => sesion.Id == id && sesion.TokenHash == hashActual &&
                sesion.RevocadoEnUtc == null && sesion.ExpiraEnUtc > ahoraUtc)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(sesion => sesion.RevocadoEnUtc, ahoraUtc),
                cancellationToken);
        return modificadas == 1;
    }
}
