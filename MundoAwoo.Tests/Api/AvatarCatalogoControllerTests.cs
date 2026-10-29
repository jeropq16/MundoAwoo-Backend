using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Application.DTOs.Catalogo;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Infrastructure.Persistence;
using static MundoAwoo.Tests.TestData;

namespace MundoAwoo.Tests.Api;

public class AvatarCatalogoControllerTests
{
    [Theory]
    [InlineData("GET", "/api/avatar")]
    [InlineData("PUT", "/api/avatar/color")]
    public async Task Avatar_SinTokenResponde401_ConTokenInvalidoResponde401(string metodo, string ruta)
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();

        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        if (metodo == "PUT") solicitud.Content = JsonContent.Create(new { colorFondo = "#123456" });
        using var sinToken = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Contains(sinToken.Headers.WwwAuthenticate, valor => valor.Scheme == "Bearer");

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "jwt-invalido");
        using var otraSolicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        if (metodo == "PUT") otraSolicitud.Content = JsonContent.Create(new { colorFondo = "#123456" });
        using var invalido = await cliente.SendAsync(otraSolicitud);
        Assert.Equal(HttpStatusCode.Unauthorized, invalido.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Avatar_SubInutilizableResponde403(string? sujeto)
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(sujeto));

        using var respuesta = await cliente.GetAsync("/api/avatar");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task ObtenerAvatar_DevuelveSoloElPropioConColorEItemsEquipados()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        Guid itemId;
        using (var scope = api.Services.CreateScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            Avatar propio = CrearAvatar(api.PrimerUsuarioId);
            Avatar ajeno = CrearAvatar(api.SegundoUsuarioId);
            Categoria categoria = CrearCategoria("Ropa", TipoCategoria.Ropa);
            ItemDeAvatar item = CrearItem("Camiseta", categoria, RanuraAvatar.Torso, "ropa/camiseta.svg");
            itemId = item.Id;
            propio.ItemsEquipados.Add(new ItemEquipado
            {
                Id = Guid.NewGuid(), AvatarId = propio.Id, ItemDeAvatarId = item.Id, Ranura = item.Ranura
            });
            contexto.Categorias.Add(categoria);
            contexto.ItemsDeAvatar.Add(item);
            contexto.Avatares.AddRange(propio, ajeno);
            await contexto.SaveChangesAsync();
        }

        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var respuesta = await cliente.GetAsync($"/api/avatar?usuarioId={api.SegundoUsuarioId}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl!.NoStore);
        AvatarDto dto = (await respuesta.Content.ReadFromJsonAsync<AvatarDto>())!;
        Assert.Equal(api.PrimerUsuarioId, dto.UsuarioId);
        Assert.Equal("#FFFFFF", dto.ColorFondo);
        ItemEquipadoDto equipado = Assert.Single(dto.ItemsEquipados);
        Assert.Equal(itemId, equipado.ItemDeAvatarId);
        Assert.Equal("Camiseta", equipado.Nombre);
        Assert.Equal("Torso", equipado.Ranura);
        Assert.Equal("ropa/camiseta.svg", equipado.RecursoVisual);
    }

    [Fact]
    public async Task CambiarColor_NormalizaPersisteYAislaElAvatarPropio()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        await CrearAvataresAsync(api);
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        using var respuesta = await cliente.PutAsJsonAsync($"/api/avatar/color?usuarioId={api.SegundoUsuarioId}",
            new { colorFondo = "  #a1b2c3  " });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("#A1B2C3", (await respuesta.Content.ReadFromJsonAsync<AvatarDto>())!.ColorFondo);
        Assert.Equal("#A1B2C3", (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!.ColorFondo);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.SegundoUsuarioId.ToString()));
        Assert.Equal("#FFFFFF", (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!.ColorFondo);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"colorFondo\":null}")]
    [InlineData("{\"colorFondo\":\"\"}")]
    [InlineData("{\"colorFondo\":\"#12345\"}")]
    [InlineData("{\"colorFondo\":\"#GGGGGG\"}")]
    [InlineData("{\"colorFondo\":42}")]
    [InlineData("{\"colorFondo\":\"#123456\",\"usuarioId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("null")]
    [InlineData("{")]
    public async Task CambiarColor_EntradaInvalidaResponde400SinGuardar(string json)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        await CrearAvataresAsync(api);
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        using var respuesta = await cliente.PutAsync("/api/avatar/color",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("#FFFFFF", (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!.ColorFondo);
    }

    [Theory]
    [InlineData("GET", "/api/avatar")]
    [InlineData("PUT", "/api/avatar/color")]
    public async Task AvatarInexistente_Responde404(string metodo, string ruta)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        if (metodo == "PUT") solicitud.Content = JsonContent.Create(new { colorFondo = "#123456" });

        using var respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        using var problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(404, problema.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Categorias_EsPublicoOrdenadoYDevuelveTipoLegible()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using (var scope = api.Services.CreateScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            contexto.Categorias.AddRange(
                CrearCategoria("Ropa", TipoCategoria.Ropa),
                CrearCategoria("Accesorios", TipoCategoria.Accesorio));
            await contexto.SaveChangesAsync();
        }
        using var cliente = api.CrearCliente();

        using var respuesta = await cliente.GetAsync("/api/catalogo/categorias");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        CategoriaDto[] categorias = (await respuesta.Content.ReadFromJsonAsync<CategoriaDto[]>())!;
        Assert.Collection(categorias,
            primera => { Assert.Equal("Accesorios", primera.Nombre); Assert.Equal("Accesorio", primera.Tipo); },
            segunda => { Assert.Equal("Ropa", segunda.Nombre); Assert.Equal("Ropa", segunda.Tipo); });
        Assert.All(categorias, categoria => Assert.NotEqual(Guid.Empty, categoria.Id));
    }

    [Fact]
    public async Task Categorias_SinDatosDevuelveListaVacia()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();

        CategoriaDto[] categorias = (await cliente.GetFromJsonAsync<CategoriaDto[]>("/api/catalogo/categorias"))!;
        Assert.Empty(categorias);
    }

    private static async Task CrearAvataresAsync(PerfilApiFactory api)
    {
        using var scope = api.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        contexto.Avatares.AddRange(CrearAvatar(api.PrimerUsuarioId), CrearAvatar(api.SegundoUsuarioId));
        await contexto.SaveChangesAsync();
    }
}
