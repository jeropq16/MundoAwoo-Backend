namespace MundoAwoo.Application.Exceptions;

// El mismo mensaje evita confirmar si el correo existe o si la cuenta está bloqueada.
public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException() : base("No se pudo iniciar sesión con esas credenciales.") { }
}
