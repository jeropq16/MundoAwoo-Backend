using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Domain.Interfaces;

// El avatar controla sus ítems equipados: los cargamos como parte del mismo agregado.
public interface IAvatarRepository
{
    // Busca el avatar de un usuario e incluye todos sus ItemsEquipados.
    // Devuelve null si ese usuario aún no tiene un avatar.
    // Los cambios del agregado devuelto quedan pendientes de confirmación
    // dentro de la unidad de trabajo de la misma operación.
    Task<Avatar?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    // Registra el avatar y sus ítems equipados como pendientes de insertar.
    // El índice único de UsuarioId en MySQL impide crear dos avatares para el mismo usuario.
    // Este método no confirma la escritura en la base de datos.
    void Agregar(Avatar avatar);
}
