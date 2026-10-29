using MundoAwoo.Application.DTOs.Autenticacion;

namespace MundoAwoo.Application.Interfaces;

// Permite emitir un token sin que el consumidor conozca la biblioteca JWT ni la clave.
public interface IJwtTokenService
{
    AutenticacionRespuestaDto Crear(Guid usuarioId);
    AutenticacionRespuestaDto Crear(Guid usuarioId, DateTimeOffset sesionExpiraEnUtc);
}
