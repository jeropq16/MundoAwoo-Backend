using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Infrastructure.Persistence;
using static MundoAwoo.Tests.TestData;

namespace MundoAwoo.Tests.Api;

public class Apariencia3DControllerTests
{
    [Fact]
    public async Task Apariencia_PersistePorPersonajeYAislaLaCuentaSinCambiarElEstilo()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        await CrearAvatares(api);
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var guardar = await cliente.PutAsJsonAsync($"/api/avatar/apariencia3d?usuarioId={api.SegundoUsuarioId}",
            new { personaje3D = " GHETTO ", pelo = " TRENZAS ", barba = "completa", tatuajes = "blackwork", colorCabello = " #d5b36a " });
        Assert.Equal(HttpStatusCode.OK, guardar.StatusCode);
        var dto = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("trenzas", dto.AparienciaGhetto.Pelo);
        Assert.Equal("completa", dto.AparienciaGhetto.Barba);
        Assert.Equal("blackwork", dto.AparienciaGhetto.Tatuajes);
        Assert.Equal("#D5B36A", dto.AparienciaGhetto.ColorCabello);
        Assert.Equal("original", dto.AparienciaQueen.Pelo);
        Assert.Equal("ninguno", dto.AparienciaQueen.Tatuajes);
        Assert.Equal("ghetto", dto.Personaje3D);
        Assert.False(dto.ChaquetaGhetto);
        Assert.Empty(dto.ItemsEquipados);

        using var guardarQueen = await cliente.PutAsJsonAsync("/api/avatar/apariencia3d",
            new { personaje3D = "queen", pelo = "cresta", barba = "afeitado", tatuajes = "estrellas", colorCabello = "#A63032" });
        Assert.Equal(HttpStatusCode.OK, guardarQueen.StatusCode);
        using var cambiar = await cliente.PutAsJsonAsync("/api/avatar/estilo3d", new { personaje3D = "queen", chaquetaGhetto = true, chaquetaQueen = true });
        Assert.Equal(HttpStatusCode.OK, cambiar.StatusCode);
        dto = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("cresta", dto.AparienciaQueen.Pelo);
        Assert.Equal("estrellas", dto.AparienciaQueen.Tatuajes);
        Assert.Equal("trenzas", dto.AparienciaGhetto.Pelo);
        Assert.Equal("blackwork", dto.AparienciaGhetto.Tatuajes);
        Assert.True(dto.ChaquetaQueen);

        using var quitar = await cliente.PutAsJsonAsync("/api/avatar/apariencia3d",
            new { personaje3D = "queen", pelo = "rapado", barba = "bigote", tatuajes = "ninguno", colorCabello = "#201813" });
        Assert.Equal(HttpStatusCode.OK, quitar.StatusCode);
        dto = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("rapado", dto.AparienciaQueen.Pelo);
        Assert.Equal("ninguno", dto.AparienciaQueen.Tatuajes);
        Assert.Equal("bigote", dto.AparienciaQueen.Barba);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.SegundoUsuarioId.ToString()));
        var ajeno = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("original", ajeno.AparienciaGhetto.Pelo);
        Assert.Equal("original", ajeno.AparienciaQueen.Pelo);
        Assert.Equal("ninguno", ajeno.AparienciaGhetto.Tatuajes);
    }

    [Theory]
    [InlineData("otro", "fade", "completa", "ninguno", "#201813")]
    [InlineData("ghetto", "desconocido", "completa", "ninguno", "#201813")]
    [InlineData("ghetto", "fade", "desconocida", "ninguno", "#201813")]
    [InlineData("ghetto", "fade", "completa", "desconocidos", "#201813")]
    [InlineData("ghetto", "fade", "completa", "ninguno", "rojo")]
    [InlineData("ghetto", null, "completa", "ninguno", "#201813")]
    public async Task Apariencia_InvalidaNoGuardaCambiosParciales(string personaje, string? pelo, string barba, string tatuajes, string color)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        await CrearAvatares(api);
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var respuesta = await cliente.PutAsJsonAsync("/api/avatar/apariencia3d",
            new { personaje3D = personaje, pelo, barba, tatuajes, colorCabello = color });
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var dto = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("original", dto.AparienciaGhetto.Pelo);
        Assert.Equal("original", dto.AparienciaGhetto.Barba);
        Assert.Equal("ninguno", dto.AparienciaGhetto.Tatuajes);
        Assert.Equal("#201813", dto.AparienciaGhetto.ColorCabello);
    }

    [Fact]
    public async Task Apariencia_ExigeTokenYAvatarPropio()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        var datos = new { personaje3D = "ghetto", pelo = "fade", barba = "candado", tatuajes = "geometrico", colorCabello = "#201813" };
        using var anonimo = await cliente.PutAsJsonAsync("/api/avatar/apariencia3d", datos);
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var sinAvatar = await cliente.PutAsJsonAsync("/api/avatar/apariencia3d", datos);
        Assert.Equal(HttpStatusCode.NotFound, sinAvatar.StatusCode);
    }

    private static async Task CrearAvatares(PerfilApiFactory api)
    {
        using var scope = api.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        contexto.Avatares.AddRange(CrearAvatar(api.PrimerUsuarioId), CrearAvatar(api.SegundoUsuarioId));
        await contexto.SaveChangesAsync();
    }
}
