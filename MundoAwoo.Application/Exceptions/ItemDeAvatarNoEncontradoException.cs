namespace MundoAwoo.Application.Exceptions;

public class ItemDeAvatarNoEncontradoException : Exception
{
    public ItemDeAvatarNoEncontradoException() : base("El ítem solicitado no existe en el catálogo.") { }
}
