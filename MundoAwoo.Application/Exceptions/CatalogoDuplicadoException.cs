namespace MundoAwoo.Application.Exceptions;

public class CatalogoDuplicadoException : Exception
{
    public CatalogoDuplicadoException(string mensaje) : base(mensaje) { }
}
