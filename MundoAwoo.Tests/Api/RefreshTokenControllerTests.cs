using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Application.DTOs.Autenticacion;
using MundoAwoo.Infrastructure.Persistence;

namespace MundoAwoo.Tests.Api;

public class RefreshTokenControllerTests
{
    [Fact]
    public async Task RegistroYLogin_CreanSesionesSeparadasDeUnaHoraConHashGuardado()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        var registro = await RegistrarAsync(cliente);
        var login = (await (await cliente.PostAsJsonAsync("/api/autenticacion/login",
            new LoginDto { Correo = "sesion@example.com", Contrasena = "Prueba-Segura-2026!" })
            ).Content.ReadFromJsonAsync<AutenticacionRespuestaDto>())!;

        Assert.NotEqual(registro.RefreshToken, login.RefreshToken);
        Assert.InRange(registro.SesionExpiraEnUtc - DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
        Assert.True(registro.ExpiraEnUtc < registro.SesionExpiraEnUtc);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        var sesiones = await db.SesionesRefreshToken.ToListAsync();
        Assert.Equal(2, sesiones.Count);
        Assert.All(sesiones, sesion => Assert.Equal(64, sesion.TokenHash.Length));
        Assert.DoesNotContain(sesiones, sesion => sesion.TokenHash == registro.RefreshToken);
    }

    [Fact]
    public async Task Refresh_RotaTokenSinExtenderLaSesionYRechazaElAnterior()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        var primero = await RegistrarAsync(cliente);

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/refresh",
            new RefreshTokenDto { RefreshToken = primero.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var segundo = (await respuesta.Content.ReadFromJsonAsync<AutenticacionRespuestaDto>())!;
        Assert.Equal(primero.UsuarioId, segundo.UsuarioId);
        Assert.Equal(primero.SesionExpiraEnUtc, segundo.SesionExpiraEnUtc);
        Assert.NotEqual(primero.RefreshToken, segundo.RefreshToken);
        Assert.NotEqual(primero.Token, segundo.Token);

        using var replay = await cliente.PostAsJsonAsync("/api/autenticacion/refresh",
            new RefreshTokenDto { RefreshToken = primero.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var tercero = await cliente.PostAsJsonAsync("/api/autenticacion/refresh",
            new RefreshTokenDto { RefreshToken = segundo.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, tercero.StatusCode);
    }

    [Fact]
    public async Task CerrarSesion_RevocaRefresh_SinAfectarOtraSesion()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        var registro = await RegistrarAsync(cliente);
        using var loginRespuesta = await cliente.PostAsJsonAsync("/api/autenticacion/login",
            new LoginDto { Correo = "sesion@example.com", Contrasena = "Prueba-Segura-2026!" });
        var login = (await loginRespuesta.Content.ReadFromJsonAsync<AutenticacionRespuestaDto>())!;

        using var cierre = await cliente.PostAsJsonAsync("/api/autenticacion/cerrar-sesion",
            new RefreshTokenDto { RefreshToken = registro.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, cierre.StatusCode);
        using var revocado = await cliente.PostAsJsonAsync("/api/autenticacion/refresh",
            new RefreshTokenDto { RefreshToken = registro.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revocado.StatusCode);
        using var vigente = await cliente.PostAsJsonAsync("/api/autenticacion/refresh",
            new RefreshTokenDto { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, vigente.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("token-invalido")]
    public async Task Refresh_TokenMalformadoResponde401(string token)
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/refresh",
            new RefreshTokenDto { RefreshToken = token });
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Refresh_SesionExpiradaResponde401()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        var registro = await RegistrarAsync(cliente);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            var sesion = await db.SesionesRefreshToken.SingleAsync();
            sesion.ExpiraEnUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/refresh",
            new RefreshTokenDto { RefreshToken = registro.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static async Task<AutenticacionRespuestaDto> RegistrarAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro",
            new RegistroDto { Correo = "sesion@example.com", Contrasena = "Prueba-Segura-2026!",
                NombreVisible = "Fan" });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<AutenticacionRespuestaDto>())!;
    }
}
