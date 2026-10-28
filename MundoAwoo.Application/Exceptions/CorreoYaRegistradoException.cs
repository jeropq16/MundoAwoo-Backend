namespace MundoAwoo.Application.Exceptions;

public class CorreoYaRegistradoException : Exception
{
    public CorreoYaRegistradoException() : base("Ya existe una cuenta con ese correo.") { }
}
