using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AwoorldDbContext _contexto;

    public UnitOfWork(AwoorldDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        // Guarda las inserciones, modificaciones y eliminaciones pendientes en este contexto.
        // EF Core utiliza una transacción cuando hace falta para este SaveChangesAsync.
        // No abarca otros contextos ni operaciones externas, como el futuro envío de correos.
        return await _contexto.SaveChangesAsync(cancellationToken);
    }
}
