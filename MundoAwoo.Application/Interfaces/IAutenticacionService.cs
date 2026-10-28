using MundoAwoo.Application.DTOs.Autenticacion;

namespace MundoAwoo.Application.Interfaces;

public interface IAutenticacionService
{
    Task<AutenticacionRespuestaDto> RegistrarAsync(RegistroDto datos, CancellationToken cancellationToken = default);
    Task<AutenticacionRespuestaDto> LoginAsync(LoginDto datos, CancellationToken cancellationToken = default);
}
