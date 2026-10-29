using Microsoft.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AwoorldDbContext _contexto;

    // La inyección de dependencias entrega el contexto de la operación actual.
    public UsuarioRepository(AwoorldDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(
        Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _contexto.Usuarios
            // EF seguirá los cambios para poder guardar ActualizarPerfil o DesbloquearItem.
            .AsTracking()
            .Include(usuario => usuario.Perfil)
            .Include(usuario => usuario.ItemsDesbloqueados)
            // Por Id esperamos una sola coincidencia; si no existe, devuelve null.
            .SingleOrDefaultAsync(usuario => usuario.Id == usuarioId, cancellationToken);
    }

    public async Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
    {
        return await _contexto.Usuarios.AsTracking()
            .SingleOrDefaultAsync(usuario => usuario.Correo == correo, cancellationToken);
    }

    public Task<bool> EsAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        => _contexto.Usuarios.AsNoTracking()
            .AnyAsync(usuario => usuario.Id == usuarioId && usuario.EsAdministrador, cancellationToken);

    public void Agregar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        // Add incluye también el perfil y los desbloqueos del usuario nuevo.
        // Solo registra cambios pendientes: aquí no se ejecuta SaveChangesAsync.
        _contexto.Usuarios.Add(usuario);
    }
}
