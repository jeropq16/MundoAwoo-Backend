using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MundoAwoo.Application.DTOs.Perfiles;

namespace MundoAwoo.Tests.Api;

public class PerfilControllerTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task SinToken_RechazaLaSolicitud(string metodo)
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), "/api/perfil");
        if (metodo == "PUT")
            solicitud.Content = JsonContent.Create(new { nombreVisible = "Fan", biografia = "" });

        using var respuesta = await cliente.SendAsync(solicitud);

        await ComprobarProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        Assert.Contains(respuesta.Headers.WwwAuthenticate, cabecera => cabecera.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("firma")]
    [InlineData("vencido")]
    [InlineData("futuro")]
    [InlineData("emisor")]
    [InlineData("destinatario")]
    [InlineData("algoritmo")]
    [InlineData("sin-firma")]
    [InlineData("sin-expiracion")]
    [InlineData("malformado")]
    public async Task TokenInvalido_NoAccedeAlServicio(string caso)
    {
        // No creamos tablas: el resultado 401 también prueba que no se consultó la base.
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();
        string token = caso == "malformado" ? "no-es-un-jwt" : api.CrearToken(Guid.NewGuid().ToString(), caso);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await cliente.GetAsync("/api/perfil");

        await ComprobarProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        Assert.Contains(respuesta.Headers.WwwAuthenticate, cabecera => cabecera.Scheme == "Bearer");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("duplicado")]
    public async Task TokenFirmadoSinIdentidadUtilizable_SeRechaza(string? sujeto)
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();
        string token = sujeto == "duplicado"
            ? api.CrearToken(Guid.NewGuid().ToString(), "duplicado")
            : api.CrearToken(sujeto);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await cliente.GetAsync("/api/perfil");

        // Dos claims sub se serializan como un array; el validador JWT lo rechaza antes
        // de ejecutar autorización. Los otros casos llegan a la política/controlador.
        HttpStatusCode esperado = sujeto == "duplicado" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        await ComprobarProblemaAsync(respuesta, esperado);
    }

    [Fact]
    public async Task Obtener_UsaElTokenAunqueLaQueryIndiqueOtroUsuario()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        using var respuesta = await cliente.GetAsync($"/api/perfil?usuarioId={api.SegundoUsuarioId}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var perfil = await respuesta.Content.ReadFromJsonAsync<PerfilUsuarioDto>();
        Assert.Equal(api.PrimerUsuarioId, perfil!.UsuarioId);
        Assert.Equal("Primer fan", perfil.NombreVisible);
        Assert.True(respuesta.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task Actualizar_GuardaElPerfilPropioYConservaElDelOtroUsuario()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        using var respuesta = await cliente.PutAsJsonAsync($"/api/perfil?usuarioId={api.SegundoUsuarioId}",
            new { nombreVisible = "  Actualizado  ", biografia = "  Mi biografía  " });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var actualizado = await respuesta.Content.ReadFromJsonAsync<PerfilUsuarioDto>();
        Assert.Equal(api.PrimerUsuarioId, actualizado!.UsuarioId);
        Assert.Equal("Actualizado", actualizado.NombreVisible);
        Assert.Equal("Mi biografía", actualizado.Biografia);
        var guardado = await cliente.GetFromJsonAsync<PerfilUsuarioDto>("/api/perfil");
        Assert.Equal("Actualizado", guardado!.NombreVisible);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.SegundoUsuarioId.ToString()));
        var segundo = await cliente.GetFromJsonAsync<PerfilUsuarioDto>("/api/perfil");
        Assert.Equal("Segundo fan", segundo!.NombreVisible);
    }

    [Fact]
    public async Task Actualizar_UsuarioIdEnElBody_SeRechazaSinCambios()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        using var respuesta = await cliente.PutAsJsonAsync("/api/perfil", new
        {
            usuarioId = api.SegundoUsuarioId,
            nombreVisible = "Intento de cambio",
            biografia = ""
        });

        await ComprobarProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        var propio = await cliente.GetFromJsonAsync<PerfilUsuarioDto>("/api/perfil");
        Assert.Equal("Primer fan", propio!.NombreVisible);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.SegundoUsuarioId.ToString()));
        Assert.Equal("Segundo fan", (await cliente.GetFromJsonAsync<PerfilUsuarioDto>("/api/perfil"))!.NombreVisible);
    }

    [Theory]
    [InlineData("{\"nombreVisible\":\"   \"}")]
    [InlineData("{\"nombreVisible\":null}")]
    [InlineData("{\"nombreVisible\":\"Fan\",\"biografia\":null}")]
    [InlineData("{\"nombreVisible\":42}")]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("")]
    public async Task SolicitudInvalida_Devuelve400SinModificarElPerfil(string json)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        using var respuesta = await cliente.PutAsync("/api/perfil", new StringContent(json, Encoding.UTF8, "application/json"));

        await ComprobarProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.Equal("Primer fan", (await cliente.GetFromJsonAsync<PerfilUsuarioDto>("/api/perfil"))!.NombreVisible);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task UsuarioDelTokenInexistente_Devuelve404(string metodo)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(Guid.NewGuid().ToString()));
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), "/api/perfil");
        if (metodo == "PUT")
            solicitud.Content = JsonContent.Create(new { nombreVisible = "Fan", biografia = "" });

        using var respuesta = await cliente.SendAsync(solicitud);

        await ComprobarProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ErrorInesperado_Devuelve500SinDetallesInternos()
    {
        using var api = new PerfilApiFactory(simularFallo: true);
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(Guid.NewGuid().ToString()));

        using var respuesta = await cliente.GetAsync("/api/perfil");

        await ComprobarProblemaAsync(respuesta, HttpStatusCode.InternalServerError);
        string cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DETALLE_INTERNO_NO_EXPONER", cuerpo);
        Assert.DoesNotContain("stackTrace", cuerpo);
        Assert.Contains("traceId", cuerpo);
    }

    private static async Task ComprobarProblemaAsync(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal((int)esperado, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("title").GetString()));
    }
}
