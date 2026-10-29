using Microsoft.AspNetCore.Authorization;
using MundoAwoo.Application.Interfaces;

namespace MundoAwoo.Api.Security;

public sealed class AdministradorRequirement : IAuthorizationRequirement;

public class AdministradorHandler : AuthorizationHandler<AdministradorRequirement>
{
    private readonly IAccesoAdministradorService _acceso;

    public AdministradorHandler(IAccesoAdministradorService acceso) => _acceso = acceso;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        AdministradorRequirement requirement)
    {
        if (UsuarioAutenticado.IntentarObtenerId(context.User, out Guid usuarioId) &&
            await _acceso.EsAdministradorAsync(usuarioId))
            context.Succeed(requirement);
    }
}
