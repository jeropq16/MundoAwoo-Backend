using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MundoAwoo.Application.DTOs.Autenticacion;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Infrastructure.Options;

namespace MundoAwoo.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opciones;

    public JwtTokenService(IOptions<JwtOptions> opciones)
    {
        _opciones = opciones.Value;
    }

    public AutenticacionRespuestaDto Crear(Guid usuarioId)
        => CrearInterno(usuarioId, null);

    public AutenticacionRespuestaDto Crear(Guid usuarioId, DateTimeOffset sesionExpiraEnUtc)
        => CrearInterno(usuarioId, sesionExpiraEnUtc);

    private AutenticacionRespuestaDto CrearInterno(Guid usuarioId, DateTimeOffset? sesionExpiraEnUtc)
    {
        DateTime ahora = DateTime.UtcNow;
        DateTime expiracion = ahora.AddMinutes(_opciones.DuracionMinutos);
        if (sesionExpiraEnUtc.HasValue && sesionExpiraEnUtc.Value.UtcDateTime < expiracion)
            expiracion = sesionExpiraEnUtc.Value.UtcDateTime;
        if (expiracion <= ahora)
            throw new InvalidOperationException("La sesión ya expiró.");
        var claims = new[]
        {
            // El controlador de perfil leerá este identificador del dominio.
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(ahora).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.SigningKey));
        var firma = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opciones.Issuer, _opciones.Audience, claims,
            notBefore: ahora, expires: expiracion, signingCredentials: firma);

        return new AutenticacionRespuestaDto
        {
            UsuarioId = usuarioId,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            // JWT expresa segundos enteros: devolvemos la expiración que realmente lleva el token.
            ExpiraEnUtc = new DateTimeOffset(token.ValidTo, TimeSpan.Zero)
        };
    }
}
