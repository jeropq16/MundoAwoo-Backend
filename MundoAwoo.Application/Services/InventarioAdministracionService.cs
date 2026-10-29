using MundoAwoo.Application.DTOs.Inventario;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;

namespace MundoAwoo.Application.Services;

public class InventarioAdministracionService : IInventarioAdministracionService
{
    private readonly IAccesoAdministradorService _acceso;
    private readonly IUsuarioRepository _usuarios;
    private readonly IItemDeAvatarRepository _items;
    private readonly IUsuarioService _reglas;
    private readonly IUnitOfWork _unidadDeTrabajo;

    public InventarioAdministracionService(IAccesoAdministradorService acceso, IUsuarioRepository usuarios,
        IItemDeAvatarRepository items, IUsuarioService reglas, IUnitOfWork unidadDeTrabajo)
    {
        _acceso = acceso;
        _usuarios = usuarios;
        _items = items;
        _reglas = reglas;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    public async Task<ItemInventarioDto> ConcederAsync(Guid administradorId, Guid usuarioId, Guid itemId,
        CancellationToken cancellationToken = default)
    {
        await _acceso.ExigirAdministradorAsync(administradorId, cancellationToken);
        if (usuarioId == Guid.Empty || itemId == Guid.Empty)
            throw new ArgumentException("El usuario y el ítem son obligatorios.");

        Usuario usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
            ?? throw new UsuarioNoEncontradoException(usuarioId);
        ItemDeAvatar item = await _items.ObtenerPorIdAsync(itemId, cancellationToken)
            ?? throw new ItemDeAvatarNoEncontradoException();

        _reglas.DesbloquearItem(usuario, item);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        ItemDesbloqueado desbloqueo = usuario.ItemsDesbloqueados.Single(x => x.ItemDeAvatarId == itemId);
        return new ItemInventarioDto
        {
            Item = CatalogoConsultaService.CrearItemDto(item),
            FechaDesbloqueo = desbloqueo.FechaDesbloqueo
        };
    }
}
