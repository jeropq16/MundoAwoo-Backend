using MundoAwoo.Application.DTOs.Inventario;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Application.Services;

public class InventarioConsultaService : IInventarioConsultaService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IItemDeAvatarRepository _items;

    public InventarioConsultaService(IUsuarioRepository usuarios, IItemDeAvatarRepository items)
    {
        _usuarios = usuarios;
        _items = items;
    }

    public async Task<IReadOnlyList<ItemInventarioDto>> ObtenerAsync(Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(usuarioId));

        Usuario usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
            ?? throw new UsuarioNoEncontradoException(usuarioId);
        var desbloqueados = usuario.ItemsDesbloqueados.OrderBy(item => item.FechaDesbloqueo)
            .ThenBy(item => item.ItemDeAvatarId).ToArray();
        var ids = desbloqueados.Select(item => item.ItemDeAvatarId).ToArray();
        IReadOnlyList<ItemDeAvatar> items = await _items.ObtenerPorIdsAsync(ids, cancellationToken);
        var porId = items.ToDictionary(item => item.Id);

        return desbloqueados.Select(desbloqueado => new ItemInventarioDto
        {
            Item = CatalogoConsultaService.CrearItemDto(porId[desbloqueado.ItemDeAvatarId]),
            FechaDesbloqueo = desbloqueado.FechaDesbloqueo
        }).ToArray();
    }
}
