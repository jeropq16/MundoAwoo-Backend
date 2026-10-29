namespace MundoAwoo.Application.Exceptions;

public class AvatarNoEncontradoException : Exception
{
    public AvatarNoEncontradoException() : base("No existe un avatar asociado al usuario autenticado.") { }
}
