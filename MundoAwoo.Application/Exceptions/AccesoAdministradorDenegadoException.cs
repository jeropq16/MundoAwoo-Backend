namespace MundoAwoo.Application.Exceptions;

public class AccesoAdministradorDenegadoException : Exception
{
    public AccesoAdministradorDenegadoException() : base("Esta operación requiere una cuenta administradora.") { }
}
