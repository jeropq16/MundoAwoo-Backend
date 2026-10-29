using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Domain.Interfaces;

// Contrato para consultar y agregar el agregado Usuario.
// La implementación del acceso a datos estará en Infrastructure.
public interface IUsuarioRepository
{
    // Debe devolver el usuario con su Perfil e ItemsDesbloqueados cargados.
    // Devuelve null cuando no existe un usuario con este identificador.
    // Los cambios realizados sobre el agregado devuelto deben poder guardarse
    // al confirmar la unidad de trabajo de la misma operación.
    Task<Usuario?> ObtenerPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default);

    Task<bool> EsAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    // Registra un usuario nuevo junto con su perfil e inventario como pendientes de insertar.
    // No confirma la escritura: se guardarán juntos al finalizar el caso de uso.
    void Agregar(Usuario usuario);
}
