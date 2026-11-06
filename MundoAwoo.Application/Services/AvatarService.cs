using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.ValueObjects;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Application.Services;

public class AvatarService : IAvatarService
{
    private readonly IUsuarioService _usuarios;

    public AvatarService(IUsuarioService usuarios) => _usuarios = usuarios;

    public Avatar Crear(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El avatar debe pertenecer a un usuario.", nameof(usuarioId));

        return new Avatar
        {
            Id = Guid.NewGuid(), UsuarioId = usuarioId,
            ColorFondo = new ColorHex("#FFFFFF")
        };
    }

    public void CambiarColorFondo(Avatar avatar, ColorHex color)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        ArgumentNullException.ThrowIfNull(color);
        avatar.ColorFondo = color;
    }

    public void CambiarEstilo3D(Avatar avatar, string personaje, bool chaquetaGhetto, bool chaquetaQueen)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        string valor = personaje?.Trim().ToLowerInvariant() ?? string.Empty;
        if (valor is not ("ghetto" or "queen"))
            throw new ArgumentException("El personaje 3D debe ser ghetto o queen.", nameof(personaje));
        avatar.Personaje3D = valor;
        avatar.ChaquetaGhetto = chaquetaGhetto;
        avatar.ChaquetaQueen = chaquetaQueen;
    }

    public void CambiarApariencia3D(Avatar avatar, string personaje, string pelo, string barba,
        string tatuajes, ColorHex colorCabello)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        ArgumentNullException.ThrowIfNull(colorCabello);
        // Validate every option before changing either character's saved appearance.
        string objetivo = ValidarOpcion(personaje, "personaje", "ghetto", "queen");
        string corte = ValidarOpcion(pelo, "pelo", "original", "rapado", "fade", "trenzas", "cresta");
        string vello = ValidarOpcion(barba, "barba", "original", "afeitado", "candado", "completa", "bigote");
        string tinta = ValidarOpcion(tatuajes, "tatuajes", "ninguno", "geometrico", "estrellas", "blackwork");
        if (objetivo == "ghetto")
        {
            avatar.PeloGhetto = corte;
            avatar.BarbaGhetto = vello;
            avatar.TatuajesGhetto = tinta;
            avatar.ColorCabelloGhetto = colorCabello.Valor;
        }
        else
        {
            avatar.PeloQueen = corte;
            avatar.BarbaQueen = vello;
            avatar.TatuajesQueen = tinta;
            avatar.ColorCabelloQueen = colorCabello.Valor;
        }
    }

    private static string ValidarOpcion(string? opcion, string nombre, params string[] permitidas)
    {
        string valor = opcion?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!permitidas.Contains(valor))
            throw new ArgumentException($"La opción de {nombre} no es válida.", nombre);
        return valor;
    }

    public void Equipar(Avatar avatar, ItemDeAvatar item, Usuario propietario)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(propietario);

        if (propietario.Id != avatar.UsuarioId)
            throw new InvalidOperationException("El usuario no es el propietario del avatar.");
        if (!_usuarios.TieneItemDesbloqueado(propietario, item.Id))
            throw new InvalidOperationException("Debes desbloquear el ítem antes de equiparlo.");
        if (avatar.ItemsEquipados.Any(equipado => equipado.ItemDeAvatarId == item.Id)) return;
        if (avatar.ItemsEquipados.Any(equipado => equipado.Ranura == item.Ranura))
            throw new InvalidOperationException("La ranura está ocupada. Desequipa su ítem primero.");

        avatar.ItemsEquipados.Add(new ItemEquipado
        {
            Id = Guid.NewGuid(), AvatarId = avatar.Id,
            ItemDeAvatarId = item.Id, Ranura = item.Ranura
        });
    }

    public void Desequipar(Avatar avatar, Guid itemDeAvatarId)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        if (itemDeAvatarId == Guid.Empty)
            throw new ArgumentException("El identificador del ítem es obligatorio.", nameof(itemDeAvatarId));

        ItemEquipado? equipado = avatar.ItemsEquipados.FirstOrDefault(item => item.ItemDeAvatarId == itemDeAvatarId);
        if (equipado is not null) avatar.ItemsEquipados.Remove(equipado);
    }
}
