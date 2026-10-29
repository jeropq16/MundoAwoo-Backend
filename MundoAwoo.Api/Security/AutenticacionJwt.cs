using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MundoAwoo.Infrastructure.Options;

namespace MundoAwoo.Api.Security;

// Agrupamos la configuración para que Program.cs conserve un recorrido fácil de leer.
public static class AutenticacionJwt
{
    public const string PoliticaPerfilPropio = "PerfilPropio";
    public const string PoliticaAdministrador = "Administrador";

    public static void Registrar(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = new JwtOptions();
        configuration.GetSection("Jwt").Bind(jwt);

        // Fallamos al iniciar si falta configuración. No hay una clave predeterminada.
        if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
            throw new InvalidOperationException("Configura Jwt:Issuer y Jwt:Audience.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
            throw new InvalidOperationException("Configura Jwt:SigningKey con una clave aleatoria de al menos 32 bytes. Consulta docs/07-api-perfil.md.");

        if (jwt.DuracionMinutos < 1 || jwt.DuracionMinutos > 60)
            throw new InvalidOperationException("Jwt:DuracionMinutos debe estar entre 1 y 60.");

        // El generador usa la misma sección que el validador, sin duplicar claves.
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Conserva los nombres originales de claims, especialmente 'sub'.
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    RequireSignedTokens = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        // Sustituimos la respuesta vacía por un error JSON sin detalles del token.
                        context.HandleResponse();
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        await Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
                            title: "Autenticación requerida",
                            detail: "Envía un token de acceso válido en Authorization: Bearer.")
                            .ExecuteAsync(context.HttpContext);
                    },
                    OnForbidden = async context =>
                    {
                        await Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                            title: "Acceso denegado",
                            detail: "El token no identifica a un usuario válido para esta operación.")
                            .ExecuteAsync(context.HttpContext);
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PoliticaPerfilPropio, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("sub");
            });
            options.AddPolicy(PoliticaAdministrador, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AdministradorRequirement());
            });
        });
    }
}
