namespace MundoAwoo.Application.DTOs.Autenticacion;

// Nunca devolvemos contraseña, hash ni la entidad de Identity.
public class AutenticacionRespuestaDto
{
    public Guid UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string TipoToken { get; set; } = "Bearer";
    public DateTimeOffset ExpiraEnUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset SesionExpiraEnUtc { get; set; }
}
