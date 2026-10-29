using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Application.Services;

public class AccesoAdministradorService : IAccesoAdministradorService
{
    private readonly IUsuarioRepository _usuarios;

    public AccesoAdministradorService(IUsuarioRepository usuarios) => _usuarios = usuarios;

    public Task<bool> EsAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        => usuarioId == Guid.Empty ? Task.FromResult(false) :
            _usuarios.EsAdministradorAsync(usuarioId, cancellationToken);

    public async Task ExigirAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
            throw new AccesoAdministradorDenegadoException();
    }
}
