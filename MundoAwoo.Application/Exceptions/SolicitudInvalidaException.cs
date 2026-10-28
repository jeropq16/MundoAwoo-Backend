namespace MundoAwoo.Application.Exceptions;

// Solo debe recibir mensajes de validación preparados para el usuario, nunca errores técnicos.
public class SolicitudInvalidaException : Exception
{
    public SolicitudInvalidaException(string mensaje) : base(mensaje) { }
}
