using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Infrastructure.Persistence;
using MundoAwoo.Domain.Enums;
using static MundoAwoo.Tests.TestData;

namespace MundoAwoo.Tests.Api;

public class Estilo3DControllerTests
{
    [Fact]
    public async Task GuardarEstilo_PersisteCadaChaquetaYNoModificaOtroUsuarioNiElInventario()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        Guid itemId;
        using (var scope = api.Services.CreateScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            var avatarPropio = CrearAvatar(api.PrimerUsuarioId);
            var categoria = CrearCategoria("Ropa 3D", TipoCategoria.Ropa);
            var item = CrearItem("Bomber de inventario", categoria, RanuraAvatar.Torso, "awoo3d:bomber");
            itemId = item.Id;
            var usuario = (await contexto.Usuarios.FindAsync(api.PrimerUsuarioId))!;
            DesbloquearItem(usuario, item);
            Equipar(avatarPropio, item, usuario);
            contexto.Categorias.Add(categoria);
            contexto.ItemsDeAvatar.Add(item);
            contexto.Avatares.AddRange(avatarPropio, CrearAvatar(api.SegundoUsuarioId));
            await contexto.SaveChangesAsync();
        }
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var guardar = await cliente.PutAsJsonAsync($"/api/avatar/estilo3d?usuarioId={api.SegundoUsuarioId}",
            new { personaje3D = " QUEEN ", chaquetaGhetto = true, chaquetaQueen = false });
        Assert.Equal(HttpStatusCode.OK, guardar.StatusCode);
        Assert.Equal("queen", (await guardar.Content.ReadFromJsonAsync<AvatarDto>())!.Personaje3D);

        // A new request/context must reload the saved settings, not tracked memory.
        var propio = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("queen", propio.Personaje3D);
        Assert.True(propio.ChaquetaGhetto);
        Assert.False(propio.ChaquetaQueen);
        Assert.Equal(itemId, Assert.Single(propio.ItemsEquipados).ItemDeAvatarId);

        using var cambiar = await cliente.PutAsJsonAsync("/api/avatar/estilo3d",
            new { personaje3D = "ghetto", chaquetaGhetto = true, chaquetaQueen = true });
        Assert.Equal(HttpStatusCode.OK, cambiar.StatusCode);
        var vuelto = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("ghetto", vuelto.Personaje3D);
        Assert.True(vuelto.ChaquetaGhetto);
        Assert.True(vuelto.ChaquetaQueen);
        Assert.Equal(itemId, Assert.Single(vuelto.ItemsEquipados).ItemDeAvatarId);
        using var quitar = await cliente.PutAsJsonAsync("/api/avatar/estilo3d",
            new { personaje3D = "queen", chaquetaGhetto = false, chaquetaQueen = false });
        Assert.Equal(HttpStatusCode.OK, quitar.StatusCode);
        var sinBomber = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.False(sinBomber.ChaquetaGhetto);
        Assert.False(sinBomber.ChaquetaQueen);
        Assert.Equal(itemId, Assert.Single(sinBomber.ItemsEquipados).ItemDeAvatarId);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.SegundoUsuarioId.ToString()));
        var ajeno = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("ghetto", ajeno.Personaje3D);
        Assert.False(ajeno.ChaquetaGhetto);
        Assert.False(ajeno.ChaquetaQueen);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"personaje3D\":null,\"chaquetaQueen\":true}")]
    [InlineData("{\"personaje3D\":\"otro\",\"chaquetaGhetto\":true}")]
    public async Task GuardarEstilo_PersonajeInvalidoNoModificaLaCuenta(string json)
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using (var scope = api.Services.CreateScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            contexto.Avatares.Add(CrearAvatar(api.PrimerUsuarioId));
            await contexto.SaveChangesAsync();
        }
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var respuesta = await cliente.PutAsync("/api/avatar/estilo3d", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var avatar = (await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!;
        Assert.Equal("ghetto", avatar.Personaje3D);
        Assert.False(avatar.ChaquetaGhetto);
        Assert.False(avatar.ChaquetaQueen);
    }

    [Fact]
    public async Task GuardarEstilo_ExigeSesionYAvatarExistente()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        using var anonimo = await cliente.PutAsJsonAsync("/api/avatar/estilo3d", new { personaje3D = "queen" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var sinAvatar = await cliente.PutAsJsonAsync("/api/avatar/estilo3d", new { personaje3D = "queen" });
        Assert.Equal(HttpStatusCode.NotFound, sinAvatar.StatusCode);
    }
}
