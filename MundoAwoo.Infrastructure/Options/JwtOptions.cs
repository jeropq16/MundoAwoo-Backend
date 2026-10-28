namespace MundoAwoo.Infrastructure.Options;

// Configuración compartida por la validación y la emisión de JWT.
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; } = 15;

    // Guardar en User Secrets o variables de entorno. Nunca en Git ni en React.
    public string SigningKey { get; set; } = string.Empty;
}
