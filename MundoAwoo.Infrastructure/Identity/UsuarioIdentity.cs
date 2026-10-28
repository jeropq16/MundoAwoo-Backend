using Microsoft.AspNetCore.Identity;

namespace MundoAwoo.Infrastructure.Identity;

// Identity administra correo, hash de contraseña, intentos fallidos y bloqueo.
public class UsuarioIdentity : IdentityUser<Guid>
{
    // Relación uno a uno con el usuario del dominio. Este Id será el sub del JWT.
    public Guid UsuarioId { get; set; }
}
