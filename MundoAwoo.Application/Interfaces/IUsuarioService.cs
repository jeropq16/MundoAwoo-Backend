using MundoAwoo.Domain.Entities;

namespace MundoAwoo.Application.Interfaces;

public interface IUsuarioService
{
    Usuario Crear(string nombreVisible);
    void ActualizarPerfil(Usuario usuario, string nombreVisible, string biografia);
    bool TieneItemDesbloqueado(Usuario usuario, Guid itemId);
    void DesbloquearItem(Usuario usuario, ItemDeAvatar item);
}
