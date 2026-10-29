using MundoAwoo.Domain.Entities;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Application.Services;

public class UsuarioService : IUsuarioService
{
    public Usuario Crear(string nombreVisible)
    {
        string nombre = ValidarNombre(nombreVisible);
        var usuario = new Usuario { Id = Guid.NewGuid(), FechaRegistro = DateTimeOffset.UtcNow };
        usuario.Perfil = new PerfilUsuario
        {
            Id = Guid.NewGuid(), UsuarioId = usuario.Id,
            NombreVisible = nombre, Biografia = string.Empty
        };
        return usuario;
    }

    public bool TieneItemDesbloqueado(Usuario usuario, Guid itemId)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        return usuario.ItemsDesbloqueados.Any(item => item.ItemDeAvatarId == itemId);
    }

    public void ActualizarPerfil(Usuario usuario, string nombreVisible, string biografia)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        string nombre = ValidarNombre(nombreVisible);
        ArgumentNullException.ThrowIfNull(biografia);
        if (biografia.Trim().Length > 500)
            throw new ArgumentException("La biografía permite hasta 500 caracteres.", nameof(biografia));

        usuario.Perfil.NombreVisible = nombre;
        usuario.Perfil.Biografia = biografia.Trim();
    }

    public void DesbloquearItem(Usuario usuario, ItemDeAvatar item)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(item);
        if (TieneItemDesbloqueado(usuario, item.Id)) return;

        usuario.ItemsDesbloqueados.Add(new ItemDesbloqueado
        {
            Id = Guid.NewGuid(), UsuarioId = usuario.Id, ItemDeAvatarId = item.Id,
            FechaDesbloqueo = DateTimeOffset.UtcNow
        });
    }

    public static string ValidarNombre(string nombreVisible)
    {
        if (string.IsNullOrWhiteSpace(nombreVisible) || nombreVisible.Trim().Length > 50)
            throw new ArgumentException("El nombre visible debe tener entre 1 y 50 caracteres.", nameof(nombreVisible));
        return nombreVisible.Trim();
    }
}
