using System.Security.Claims;

namespace MundoAwoo.Api.Security;

public static class UsuarioAutenticado
{
    public static bool IntentarObtenerId(ClaimsPrincipal usuario, out Guid usuarioId)
    {
        usuarioId = Guid.Empty;
        var identificadores = usuario.FindAll("sub").ToList();
        return identificadores.Count == 1
            && Guid.TryParse(identificadores[0].Value, out usuarioId)
            && usuarioId != Guid.Empty;
    }
}
