namespace MundoAwoo.Application.Exceptions;

public class CategoriaNoEncontradaException : Exception
{
    public CategoriaNoEncontradaException() : base("La categoría indicada no existe.") { }
}
