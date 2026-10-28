namespace MundoAwoo.Application.DTOs.Perfiles;

// DTO de salida: una copia de los datos que queremos entregar al cliente.
// No exponemos la entidad completa ni su inventario.
public class PerfilUsuarioDto
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string NombreVisible { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
}
