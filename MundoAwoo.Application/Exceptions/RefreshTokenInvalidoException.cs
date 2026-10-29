namespace MundoAwoo.Application.Exceptions;

public class RefreshTokenInvalidoException : Exception
{
    public RefreshTokenInvalidoException() : base("El refresh token no es válido o la sesión expiró.") { }
}
