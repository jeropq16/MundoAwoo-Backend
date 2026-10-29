using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.DTOs.Inventario;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Api.Controllers;

[ApiController]
[Route("api/inventario")]
[Authorize(Policy = AutenticacionJwt.PoliticaPerfilPropio)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class InventarioController : ControllerBase
{
    private readonly IInventarioConsultaService _inventario;
    private readonly IInventarioAdministracionService _administracion;

    public InventarioController(IInventarioConsultaService inventario,
        IInventarioAdministracionService administracion)
    {
        _inventario = inventario;
        _administracion = administracion;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ItemInventarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ItemInventarioDto>>> Obtener(CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid usuarioId)) return Forbid();
        return Ok(await _inventario.ObtenerAsync(usuarioId, cancellationToken));
    }

    [HttpPost("items/{itemId}/desbloquear")]
    [Authorize(Policy = AutenticacionJwt.PoliticaAdministrador)]
    [ProducesResponseType(typeof(ItemInventarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemInventarioDto>> Desbloquear(
        Guid itemId, [FromBody] ConcederItemDto datos, CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid administradorId)) return Forbid();
        if (datos is null || datos.UsuarioId == Guid.Empty || itemId == Guid.Empty)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Datos inválidos",
                Detail = "Indica un usuarioId y un itemId válidos." });

        return Ok(await _administracion.ConcederAsync(administradorId, datos.UsuarioId, itemId, cancellationToken));
    }
}
