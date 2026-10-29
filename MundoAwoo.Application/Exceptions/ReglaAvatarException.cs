namespace MundoAwoo.Application.Exceptions;

public class ReglaAvatarException : InvalidOperationException
{
    public ReglaAvatarException(string mensaje) : base(mensaje) { }
}
