using Microsoft.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Infrastructure.Persistence.Repositories;

public class AvatarRepository : IAvatarRepository
{
    private readonly AwoorldDbContext _contexto;

    public AvatarRepository(AwoorldDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<Avatar?> ObtenerPorUsuarioIdAsync(
        Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _contexto.Avatares
            .AsTracking()
            // Necesitamos todas las ranuras ocupadas para validar Equipar correctamente.
            .Include(avatar => avatar.ItemsEquipados)
            .SingleOrDefaultAsync(avatar => avatar.UsuarioId == usuarioId, cancellationToken);
    }

    public void Agregar(Avatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);

        // El avatar y su equipamiento quedan pendientes de insertar juntos.
        _contexto.Avatares.Add(avatar);
    }
}
