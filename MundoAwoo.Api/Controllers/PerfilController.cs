using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.DTOs.Perfiles;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Api.Controllers;

[ApiController]
[Route("api/perfil")]
[Authorize(Policy = AutenticacionJwt.PoliticaPerfilPropio)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PerfilController : ControllerBase
{
    private readonly IPerfilService _perfiles;

    public PerfilController(IPerfilService perfiles)
    {
        _perfiles = perfiles;
    }

    [HttpGet]
    public async Task<ActionResult<PerfilUsuarioDto>> Obtener(CancellationToken cancellationToken)
    {
        // El cliente no elige un UsuarioId por URL o query string: usamos el token validado.
        if (!IntentarObtenerUsuarioId(out Guid usuarioId))
            return Forbid();

        PerfilUsuarioDto perfil = await _perfiles.ObtenerAsync(usuarioId, cancellationToken);
        return Ok(perfil);
    }

    [HttpPut]
    public async Task<ActionResult<PerfilUsuarioDto>> Actualizar(
        [FromBody] ActualizarPerfilDto datos, CancellationToken cancellationToken)
    {
        if (!IntentarObtenerUsuarioId(out Guid usuarioId))
            return Forbid();

        // Application valida con Domain y confirma el guardado antes de responder.
        PerfilUsuarioDto perfil = await _perfiles.ActualizarAsync(usuarioId, datos, cancellationToken);
        return Ok(perfil);
    }

    private bool IntentarObtenerUsuarioId(out Guid usuarioId)
    {
        usuarioId = Guid.Empty;
        var identificadores = User.FindAll("sub").ToList();

        // 'sub' deberá ser el Usuario.Id del dominio. Rechazamos una identidad ambigua.
        return identificadores.Count == 1
            && Guid.TryParse(identificadores[0].Value, out usuarioId)
            && usuarioId != Guid.Empty;
    }
}
