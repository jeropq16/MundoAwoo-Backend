using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Api.Controllers;

[ApiController]
[Route("api/avatar")]
[Authorize(Policy = AutenticacionJwt.PoliticaPerfilPropio)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AvatarController : ControllerBase
{
    private readonly IAvatarAplicacionService _avatares;

    public AvatarController(IAvatarAplicacionService avatares) => _avatares = avatares;

    [HttpGet]
    [ProducesResponseType(typeof(AvatarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvatarDto>> Obtener(CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid usuarioId)) return Forbid();
        return Ok(await _avatares.ObtenerAsync(usuarioId, cancellationToken));
    }

    [HttpPut("color")]
    [ProducesResponseType(typeof(AvatarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvatarDto>> CambiarColorFondo(
        [FromBody] CambiarColorFondoDto datos, CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid usuarioId)) return Forbid();
        return Ok(await _avatares.CambiarColorFondoAsync(usuarioId, datos, cancellationToken));
    }

    [HttpPost("items/{itemId}/equipar")]
    [ProducesResponseType(typeof(AvatarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AvatarDto>> Equipar(Guid itemId, CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid usuarioId)) return Forbid();
        return Ok(await _avatares.EquiparAsync(usuarioId, itemId, cancellationToken));
    }

    [HttpDelete("items/{itemId}/desequipar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desequipar(Guid itemId, CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid usuarioId)) return Forbid();
        await _avatares.DesequiparAsync(usuarioId, itemId, cancellationToken);
        return NoContent();
    }
}
