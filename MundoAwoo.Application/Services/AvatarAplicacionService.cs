using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;
using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Application.Services;

// Coordina las consultas y cambios del avatar propio. Las reglas de cambio siguen en AvatarService.
public class AvatarAplicacionService : IAvatarAplicacionService
{
    private readonly IAvatarRepository _avatares;
    private readonly IItemDeAvatarRepository _items;
    private readonly IAvatarService _avatarService;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IUsuarioRepository _usuarios;

    public AvatarAplicacionService(IAvatarRepository avatares, IItemDeAvatarRepository items,
        IAvatarService avatarService, IUnitOfWork unidadDeTrabajo, IUsuarioRepository usuarios)
    {
        _avatares = avatares;
        _items = items;
        _avatarService = avatarService;
        _unidadDeTrabajo = unidadDeTrabajo;
        _usuarios = usuarios;
    }

    public async Task<AvatarDto> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        Avatar avatar = await BuscarAvatarAsync(usuarioId, cancellationToken);
        return await CrearDtoAsync(avatar, cancellationToken);
    }

    public async Task<AvatarDto> CambiarColorFondoAsync(Guid usuarioId, CambiarColorFondoDto datos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        // Se valida antes de modificar el agregado; ColorHex normaliza a #RRGGBB.
        var color = new ColorHex(datos.ColorFondo);
        Avatar avatar = await BuscarAvatarAsync(usuarioId, cancellationToken);
        _avatarService.CambiarColorFondo(avatar, color);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return await CrearDtoAsync(avatar, cancellationToken);
    }

    public async Task<AvatarDto> CambiarEstilo3DAsync(Guid usuarioId, CambiarEstilo3DDto datos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        Avatar avatar = await BuscarAvatarAsync(usuarioId, cancellationToken);
        _avatarService.CambiarEstilo3D(avatar, datos.Personaje3D, datos.ChaquetaGhetto, datos.ChaquetaQueen);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return await CrearDtoAsync(avatar, cancellationToken);
    }

    public async Task<AvatarDto> CambiarApariencia3DAsync(Guid usuarioId, CambiarApariencia3DDto datos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        var color = new ColorHex(datos.ColorCabello);
        Avatar avatar = await BuscarAvatarAsync(usuarioId, cancellationToken);
        _avatarService.CambiarApariencia3D(avatar, datos.Personaje3D, datos.Pelo, datos.Barba, datos.Tatuajes, color);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return await CrearDtoAsync(avatar, cancellationToken);
    }

    public async Task<AvatarDto> EquiparAsync(Guid usuarioId, Guid itemId,
        CancellationToken cancellationToken = default)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("El identificador del ítem es obligatorio.", nameof(itemId));

        Avatar avatar = await BuscarAvatarAsync(usuarioId, cancellationToken);
        ItemDeAvatar item = await _items.ObtenerPorIdAsync(itemId, cancellationToken)
            ?? throw new ItemDeAvatarNoEncontradoException();
        Usuario propietario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
            ?? throw new UsuarioNoEncontradoException(usuarioId);

        try
        {
            _avatarService.Equipar(avatar, item, propietario);
        }
        catch (InvalidOperationException excepcion)
        {
            throw new ReglaAvatarException(excepcion.Message);
        }
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return await CrearDtoAsync(avatar, cancellationToken);
    }

    public async Task DesequiparAsync(Guid usuarioId, Guid itemId,
        CancellationToken cancellationToken = default)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("El identificador del ítem es obligatorio.", nameof(itemId));

        Avatar avatar = await BuscarAvatarAsync(usuarioId, cancellationToken);
        _avatarService.Desequipar(avatar, itemId);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
    }

    private async Task<Avatar> BuscarAvatarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));

        Avatar? avatar = await _avatares.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken);
        return avatar ?? throw new AvatarNoEncontradoException();
    }

    private async Task<AvatarDto> CrearDtoAsync(Avatar avatar, CancellationToken cancellationToken)
    {
        var equipados = avatar.ItemsEquipados
            .OrderBy(item => item.Ranura)
            .ThenBy(item => item.ItemDeAvatarId)
            .ToList();
        var ids = equipados.Select(item => item.ItemDeAvatarId).ToArray();
        IReadOnlyList<ItemDeAvatar> items = await _items.ObtenerPorIdsAsync(ids, cancellationToken);
        var porId = items.ToDictionary(item => item.Id);

        return new AvatarDto
        {
            Id = avatar.Id,
            UsuarioId = avatar.UsuarioId,
            ColorFondo = avatar.ColorFondo.Valor,
            Personaje3D = avatar.Personaje3D,
            ChaquetaGhetto = avatar.ChaquetaGhetto,
            ChaquetaQueen = avatar.ChaquetaQueen,
            AparienciaGhetto = new Apariencia3DDto
            {
                Pelo = avatar.PeloGhetto, Barba = avatar.BarbaGhetto,
                Tatuajes = avatar.TatuajesGhetto, ColorCabello = avatar.ColorCabelloGhetto
            },
            AparienciaQueen = new Apariencia3DDto
            {
                Pelo = avatar.PeloQueen, Barba = avatar.BarbaQueen,
                Tatuajes = avatar.TatuajesQueen, ColorCabello = avatar.ColorCabelloQueen
            },
            ItemsEquipados = equipados.Select(equipado =>
            {
                ItemDeAvatar item = porId[equipado.ItemDeAvatarId];
                return new ItemEquipadoDto
                {
                    ItemDeAvatarId = item.Id,
                    Nombre = item.Nombre,
                    Ranura = equipado.Ranura.ToString(),
                    RecursoVisual = item.RecursoVisual
                };
            }).ToArray()
        };
    }
}
