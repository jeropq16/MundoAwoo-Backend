using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.DTOs.Catalogo;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Api.Controllers;

[ApiController]
[Route("api/admin/catalogo")]
[Authorize(Policy = AutenticacionJwt.PoliticaAdministrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminCatalogoController : ControllerBase
{
    private readonly ICatalogoAdministracionService _catalogo;

    public AdminCatalogoController(ICatalogoAdministracionService catalogo) => _catalogo = catalogo;

    [HttpPost("categorias")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaDto>> CrearCategoria(
        [FromBody] CrearCategoriaDto datos, CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid administradorId)) return Forbid();
        CategoriaDto categoria = await _catalogo.CrearCategoriaAsync(administradorId, datos, cancellationToken);
        return Created($"/api/catalogo/categorias", categoria);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ItemCatalogoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ItemCatalogoDto>> CrearItem(
        [FromBody] CrearItemCatalogoDto datos, CancellationToken cancellationToken)
    {
        if (!UsuarioAutenticado.IntentarObtenerId(User, out Guid administradorId)) return Forbid();
        ItemCatalogoDto item = await _catalogo.CrearItemAsync(administradorId, datos, cancellationToken);
        return Created($"/api/catalogo/items?categoriaId={item.CategoriaId}", item);
    }
}
