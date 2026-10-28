using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MundoAwoo.Application.DTOs.Autenticacion;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
[AllowAnonymous] // Registrarse e iniciar sesión no requieren un token previo.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AutenticacionController : ControllerBase
{
    private readonly IAutenticacionService _autenticacion;

    public AutenticacionController(IAutenticacionService autenticacion)
    {
        _autenticacion = autenticacion;
    }

    [HttpPost("registro")]
    public async Task<ActionResult<AutenticacionRespuestaDto>> Registrar(
        [FromBody] RegistroDto datos, CancellationToken cancellationToken)
    {
        AutenticacionRespuestaDto respuesta = await _autenticacion.RegistrarAsync(datos, cancellationToken);
        // La cuenta ya se guardó. El token permite consultar el perfil recién creado.
        return Created("/api/perfil", respuesta);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AutenticacionRespuestaDto>> Login(
        [FromBody] LoginDto datos, CancellationToken cancellationToken)
    {
        AutenticacionRespuestaDto respuesta = await _autenticacion.LoginAsync(datos, cancellationToken);
        return Ok(respuesta);
    }
}
