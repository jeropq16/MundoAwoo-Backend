namespace MundoAwoo.Application.Exceptions;

// Distingue un usuario inexistente de un error de conexión o de datos inválidos.
// Más adelante, la API podrá traducir esta excepción a una respuesta HTTP 404.
public class UsuarioNoEncontradoException : Exception
{
    public UsuarioNoEncontradoException(Guid usuarioId)
        : base($"No existe un usuario con el identificador {usuarioId}.")
    {
    }
}
