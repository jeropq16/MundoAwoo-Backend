using MundoAwoo.Application.Services;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Tests;

// Los tests construyen datos mediante los mismos servicios que usa la aplicación.
public static class TestData
{
    private static readonly UsuarioService Usuarios = new();
    private static readonly AvatarService Avatares = new(Usuarios);
    private static readonly CatalogoService Catalogo = new();

    public static Usuario CrearUsuario(string nombre) => Usuarios.Crear(nombre);

    public static Usuario CrearUsuario(string nombre, string correo, string hash)
    {
        var usuario = Usuarios.Crear(nombre);
        usuario.Correo = correo.Trim().ToLowerInvariant();
        usuario.PasswordHash = hash;
        return usuario;
    }

    public static Avatar CrearAvatar(Guid usuarioId) => Avatares.Crear(usuarioId);
    public static Categoria CrearCategoria(string nombre, TipoCategoria tipo) => Catalogo.CrearCategoria(nombre, tipo);
    public static ItemDeAvatar CrearItem(string nombre, Categoria categoria, RanuraAvatar ranura, string recurso)
        => Catalogo.CrearItem(nombre, categoria, ranura, recurso);
    public static void ActualizarPerfil(Usuario usuario, string nombre, string biografia)
        => Usuarios.ActualizarPerfil(usuario, nombre, biografia);
    public static void DesbloquearItem(Usuario usuario, ItemDeAvatar item) => Usuarios.DesbloquearItem(usuario, item);
    public static bool TieneItemDesbloqueado(Usuario usuario, Guid itemId) => Usuarios.TieneItemDesbloqueado(usuario, itemId);
    public static void Equipar(Avatar avatar, ItemDeAvatar item, Usuario usuario) => Avatares.Equipar(avatar, item, usuario);
    public static void Desequipar(Avatar avatar, Guid itemId) => Avatares.Desequipar(avatar, itemId);
    public static void CambiarColorFondo(Avatar avatar, ColorHex color) => Avatares.CambiarColorFondo(avatar, color);
}
