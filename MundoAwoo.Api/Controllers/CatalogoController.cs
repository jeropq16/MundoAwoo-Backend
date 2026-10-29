using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MundoAwoo.Application.DTOs.Catalogo;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public class CatalogoController : ControllerBase
{
    private readonly ICatalogoConsultaService _catalogo;

    public CatalogoController(ICatalogoConsultaService catalogo) => _catalogo = catalogo;

    [HttpGet("categorias")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> ObtenerCategorias(CancellationToken cancellationToken)
    {
        return Ok(await _catalogo.ObtenerCategoriasAsync(cancellationToken));
    }

    [HttpGet("items")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ItemCatalogoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ItemCatalogoDto>>> ObtenerItems(
        [FromQuery] Guid? categoriaId, CancellationToken cancellationToken)
    {
        if (categoriaId is null || categoriaId == Guid.Empty)
            return BadRequest(new ProblemDetails { Status = 400, Title = "Categoría obligatoria",
                Detail = "Envía un categoriaId válido en la consulta." });

        return Ok(await _catalogo.ObtenerItemsAsync(categoriaId.Value, cancellationToken));
    }
}
