namespace MundoAwoo.Application.DTOs.Perfiles;

// DTO de entrada: contiene únicamente los campos que el usuario puede editar.
// El identificador del propietario se recibe aparte, desde la identidad autenticada.
public class ActualizarPerfilDto
{
    public string NombreVisible { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
}
