namespace MundoAwoo.Application.DTOs.Autenticacion;

// Datos que el usuario puede enviar para crear su cuenta. No acepta Id ni roles.
public class RegistroDto
{
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string NombreVisible { get; set; } = string.Empty;
}
