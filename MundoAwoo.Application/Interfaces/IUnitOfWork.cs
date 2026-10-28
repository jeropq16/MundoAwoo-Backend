namespace MundoAwoo.Application.Interfaces;

// Application decide cuándo confirmar todos los cambios de un caso de uso.
// Este contrato no depende de EF Core ni de MySQL.
public interface IUnitOfWork
{
    // Devuelve la cantidad de entradas de entidades escritas por EF Core.
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
