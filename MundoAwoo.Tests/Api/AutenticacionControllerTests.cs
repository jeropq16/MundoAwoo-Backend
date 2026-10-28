using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Application.DTOs.Autenticacion;
using MundoAwoo.Application.DTOs.Perfiles;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Infrastructure.Identity;
using MundoAwoo.Infrastructure.Persistence;

namespace MundoAwoo.Tests.Api;

public class AutenticacionControllerTests
{
    private const string Correo = "fan@example.com";
    private const string Contrasena = "Awoo-Pruebas-2026!";

    [Fact]
    public async Task Registro_CreaCuentaPerfilAvatarYTokenQuePermiteConsultarElPerfil()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro", DatosRegistro());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/perfil", respuesta.Headers.Location!.ToString());
        Assert.True(respuesta.Headers.CacheControl!.NoStore);
        var autenticacion = (await respuesta.Content.ReadFromJsonAsync<AutenticacionRespuestaDto>())!;
        Assert.Equal("Bearer", autenticacion.TipoToken);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(autenticacion.Token);
        Assert.Equal(autenticacion.UsuarioId.ToString(), token.Subject);
        Assert.Equal(new DateTimeOffset(token.ValidTo, TimeSpan.Zero), autenticacion.ExpiraEnUtc);
        Assert.InRange(autenticacion.ExpiraEnUtc - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(16));
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
        string cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Contrasena, cuerpo);
        Assert.DoesNotContain("passwordHash", cuerpo, StringComparison.OrdinalIgnoreCase);

        using var scope = api.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        var cuenta = await contexto.Users.SingleAsync();
        Assert.Equal(autenticacion.UsuarioId, cuenta.UsuarioId);
        Assert.NotEqual(cuenta.Id, cuenta.UsuarioId);
        Assert.Equal("FAN@EXAMPLE.COM", cuenta.NormalizedEmail);
        Assert.NotEqual(Contrasena, cuenta.PasswordHash);
        var gestor = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        Assert.True(await gestor.CheckPasswordAsync(cuenta, Contrasena));
        Assert.Equal(1, await contexto.PerfilesUsuario.CountAsync(p => p.UsuarioId == cuenta.UsuarioId));
        var avatar = await contexto.Avatares.Include(a => a.ItemsEquipados).SingleAsync(a => a.UsuarioId == cuenta.UsuarioId);
        Assert.Empty(avatar.ItemsEquipados);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", autenticacion.Token);
        var perfil = await cliente.GetFromJsonAsync<PerfilUsuarioDto>("/api/perfil");
        Assert.Equal(autenticacion.UsuarioId, perfil!.UsuarioId);
        Assert.Equal("Nuevo fan", perfil.NombreVisible);
    }

    [Fact]
    public async Task Login_NormalizaElCorreoYEntregaUnTokenNuevoSinCookies()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        var registro = await RegistrarAsync(cliente);

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/login",
            new LoginDto { Correo = "  FAN@EXAMPLE.COM  ", Contrasena = Contrasena });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl!.NoStore);
        Assert.False(respuesta.Headers.Contains("Set-Cookie"));
        var login = (await respuesta.Content.ReadFromJsonAsync<AutenticacionRespuestaDto>())!;
        Assert.Equal(registro.UsuarioId, login.UsuarioId);
        Assert.NotEqual(registro.Token, login.Token);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        using var perfil = await cliente.PutAsJsonAsync("/api/perfil", new { nombreVisible = "Con login", biografia = "" });
        Assert.Equal(HttpStatusCode.OK, perfil.StatusCode);
    }

    [Theory]
    [InlineData("fan@example.com")]
    [InlineData("FAN@EXAMPLE.COM")]
    [InlineData("  fan@example.com  ")]
    public async Task Registro_CorreoDuplicado_NoAgregaOtroUsuario(string correo)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        await RegistrarAsync(cliente);
        var datos = DatosRegistro();
        datos.Correo = correo;

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro", datos);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using var scope = api.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        Assert.Equal(1, await contexto.Users.CountAsync());
        Assert.Equal(3, await contexto.Usuarios.CountAsync()); // Dos usuarios preparados + el registrado.
        Assert.Equal(3, await contexto.PerfilesUsuario.CountAsync());
        Assert.Equal(1, await contexto.Avatares.CountAsync());
    }

    [Theory]
    [InlineData("sin-correo", Contrasena, "Fan")]
    [InlineData("fan@example.com", "abc", "Fan")]
    [InlineData("fan@example.com", "minusculaslargas", "Fan")]
    [InlineData("fan@example.com", Contrasena, "   ")]
    [InlineData("fan@example.com", null, "Fan")]
    [InlineData(null, Contrasena, "Fan")]
    public async Task Registro_DatosInvalidos_NoDejaFilasParciales(string? correo, string? contrasena, string nombre)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro",
            new { correo, contrasena, nombreVisible = nombre });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await ComprobarSinRegistroAsync(api);
    }

    [Fact]
    public async Task Registro_NoAceptaRolesNiIdentificadoresEnElCuerpo()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro",
            new { correo = Correo, contrasena = Contrasena, nombreVisible = "Fan", rol = "Admin", usuarioId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await ComprobarSinRegistroAsync(api);
    }

    [Fact]
    public async Task Registro_FalloDespuesDeGuardarIdentity_RevierteCuentaPerfilYAvatar()
    {
        using var api = new PerfilApiFactory(simularFalloToken: true);
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro", DatosRegistro());

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        await ComprobarSinRegistroAsync(api);
    }

    [Fact]
    public async Task Login_DesconocidoOContrasenaIncorrecta_DevuelveElMismoError()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        await RegistrarAsync(cliente);

        using var incorrecta = await cliente.PostAsJsonAsync("/api/autenticacion/login",
            new LoginDto { Correo = Correo, Contrasena = "Incorrecta-2026!" });
        using var desconocido = await cliente.PostAsJsonAsync("/api/autenticacion/login",
            new LoginDto { Correo = "desconocido@example.com", Contrasena = Contrasena });

        Assert.Equal(HttpStatusCode.Unauthorized, incorrecta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, desconocido.StatusCode);
        Assert.Equal("application/problem+json", incorrecta.Content.Headers.ContentType!.MediaType);
        using var errorUno = JsonDocument.Parse(await incorrecta.Content.ReadAsStringAsync());
        using var errorDos = JsonDocument.Parse(await desconocido.Content.ReadAsStringAsync());
        Assert.Equal(errorUno.RootElement.GetProperty("detail").GetString(), errorDos.RootElement.GetProperty("detail").GetString());
        Assert.False(errorUno.RootElement.TryGetProperty("token", out _));
    }

    [Fact]
    public async Task Login_CincoFallos_BloqueanTemporalmenteYDespuesPermitenElAcceso()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        await RegistrarAsync(cliente);
        for (int intento = 0; intento < 5; intento++)
        {
            using var fallo = await cliente.PostAsJsonAsync("/api/autenticacion/login",
                new LoginDto { Correo = Correo, Contrasena = "Incorrecta-2026!" });
            Assert.Equal(HttpStatusCode.Unauthorized, fallo.StatusCode);
        }

        using var bloqueado = await cliente.PostAsJsonAsync("/api/autenticacion/login",
            new LoginDto { Correo = Correo, Contrasena = Contrasena });
        Assert.Equal(HttpStatusCode.Unauthorized, bloqueado.StatusCode);

        // Adelantamos el fin del bloqueo en la base de prueba; no esperamos cinco minutos.
        using (var scope = api.Services.CreateScope())
        {
            var gestor = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
            var cuenta = (await gestor.FindByEmailAsync(Correo))!;
            Assert.True(await gestor.IsLockedOutAsync(cuenta));
            Assert.True((await gestor.SetLockoutEndDateAsync(cuenta, DateTimeOffset.UtcNow.AddMinutes(-1))).Succeeded);
        }

        using var permitido = await cliente.PostAsJsonAsync("/api/autenticacion/login",
            new LoginDto { Correo = Correo, Contrasena = Contrasena });
        Assert.Equal(HttpStatusCode.OK, permitido.StatusCode);
    }

    [Fact]
    public async Task BaseDeDatos_ImpideCorreosNormalizadosDuplicadosAunqueSeOmitaUserManager()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        await RegistrarAsync(cliente);
        using var scope = api.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        var otro = new Usuario("Otro");
        contexto.Usuarios.Add(otro);
        contexto.Users.Add(new UsuarioIdentity
        {
            Id = Guid.NewGuid(), UsuarioId = otro.Id,
            UserName = "otro", NormalizedUserName = "OTRO",
            Email = Correo, NormalizedEmail = "FAN@EXAMPLE.COM"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    private static RegistroDto DatosRegistro()
    {
        return new RegistroDto { Correo = Correo, Contrasena = Contrasena, NombreVisible = "  Nuevo fan  " };
    }

    private static async Task<AutenticacionRespuestaDto> RegistrarAsync(HttpClient cliente)
    {
        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro", DatosRegistro());
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<AutenticacionRespuestaDto>())!;
    }

    private static async Task ComprobarSinRegistroAsync(PerfilApiFactory api)
    {
        using var scope = api.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        Assert.Equal(0, await contexto.Users.CountAsync());
        Assert.Equal(2, await contexto.Usuarios.CountAsync());
        Assert.Equal(2, await contexto.PerfilesUsuario.CountAsync());
        Assert.Equal(0, await contexto.Avatares.CountAsync());
    }
}
