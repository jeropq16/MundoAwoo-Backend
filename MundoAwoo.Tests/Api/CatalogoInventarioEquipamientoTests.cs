using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Application.DTOs.Catalogo;
using MundoAwoo.Application.DTOs.Inventario;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Infrastructure.Persistence;
using static MundoAwoo.Tests.TestData;

namespace MundoAwoo.Tests.Api;

public class CatalogoInventarioEquipamientoTests
{
    [Theory]
    [InlineData("GET", "/api/inventario")]
    [InlineData("POST", "/api/avatar/items/00000000-0000-0000-0000-000000000001/equipar")]
    [InlineData("DELETE", "/api/avatar/items/00000000-0000-0000-0000-000000000001/desequipar")]
    public async Task RutasPropias_SinTokenResponden401(string metodo, string ruta)
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        using var respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/avatar/items/no-guid/equipar")]
    [InlineData("DELETE", "/api/avatar/items/no-guid/desequipar")]
    [InlineData("POST", "/api/avatar/items/00000000-0000-0000-0000-000000000000/equipar")]
    public async Task ItemIdInvalido_Responde400(string metodo, string ruta)
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(Guid.NewGuid().ToString()));
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        using var respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CatalogoItems_FiltraPorCategoriaYValidaLaQuery()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        Guid categoriaId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            Categoria ropa = CrearCategoria("Ropa", TipoCategoria.Ropa);
            Categoria accesorios = CrearCategoria("Accesorios", TipoCategoria.Accesorio);
            categoriaId = ropa.Id;
            db.Categorias.AddRange(ropa, accesorios);
            db.ItemsDeAvatar.AddRange(
                CrearItem("Zapatos", ropa, RanuraAvatar.Pies, "zapatos.svg"),
                CrearItem("Camiseta", ropa, RanuraAvatar.Torso, "camiseta.svg"),
                CrearItem("Gafas", accesorios, RanuraAvatar.Ojos, "gafas.svg"));
            await db.SaveChangesAsync();
        }
        using var cliente = api.CrearCliente();

        var items = (await cliente.GetFromJsonAsync<ItemCatalogoDto[]>(
            $"/api/catalogo/items?categoriaId={categoriaId}"))!;
        Assert.Equal(new[] { "Camiseta", "Zapatos" }, items.Select(item => item.Nombre));
        Assert.All(items, item => Assert.Equal(categoriaId, item.CategoriaId));
        Assert.Equal("Torso", items[0].Ranura);
        Assert.Equal("camiseta.svg", items[0].RecursoVisual);
        Assert.Empty((await cliente.GetFromJsonAsync<ItemCatalogoDto[]>(
            $"/api/catalogo/items?categoriaId={Guid.NewGuid()}"))!);
        using var sinFiltro = await cliente.GetAsync("/api/catalogo/items");
        using var malFiltro = await cliente.GetAsync("/api/catalogo/items?categoriaId=no-guid");
        Assert.Equal(HttpStatusCode.BadRequest, sinFiltro.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malFiltro.StatusCode);
    }

    [Fact]
    public async Task InventarioYEquipamiento_RespetanPropiedadDesbloqueoRanurasEIdempotencia()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        Guid itemId, otroId, bloqueadoId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            Categoria ropa = CrearCategoria("Ropa", TipoCategoria.Ropa);
            var item = CrearItem("Camiseta", ropa, RanuraAvatar.Torso, "camiseta.svg");
            var otro = CrearItem("Otra camiseta", ropa, RanuraAvatar.Torso, "otra.svg");
            var bloqueado = CrearItem("Zapatos", ropa, RanuraAvatar.Pies, "zapatos.svg");
            itemId = item.Id; otroId = otro.Id; bloqueadoId = bloqueado.Id;
            db.Categorias.Add(ropa);
            db.ItemsDeAvatar.AddRange(item, otro, bloqueado);
            db.Avatares.AddRange(CrearAvatar(api.PrimerUsuarioId), CrearAvatar(api.SegundoUsuarioId));
            db.ItemsDesbloqueados.AddRange(
                new ItemDesbloqueado { Id = Guid.NewGuid(), UsuarioId = api.PrimerUsuarioId,
                    ItemDeAvatarId = itemId, FechaDesbloqueo = DateTimeOffset.UtcNow },
                new ItemDesbloqueado { Id = Guid.NewGuid(), UsuarioId = api.PrimerUsuarioId,
                    ItemDeAvatarId = otroId, FechaDesbloqueo = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        using var cliente = api.CrearCliente();
        using var sinToken = await cliente.GetAsync("/api/inventario");
        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        var inventario = (await cliente.GetFromJsonAsync<ItemInventarioDto[]>(
            $"/api/inventario?usuarioId={api.SegundoUsuarioId}"))!;
        Assert.Equal(2, inventario.Length);
        Assert.Contains(inventario, entrada => entrada.Item.Id == itemId && entrada.Item.Nombre == "Camiseta");
        Assert.All(inventario, entrada => Assert.NotEqual(default, entrada.FechaDesbloqueo));

        using var respuestaBloqueado = await cliente.PostAsync($"/api/avatar/items/{bloqueadoId}/equipar", null);
        Assert.Equal(HttpStatusCode.Conflict, respuestaBloqueado.StatusCode);
        using var inexistente = await cliente.PostAsync($"/api/avatar/items/{Guid.NewGuid()}/equipar", null);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        using var equipado = await cliente.PostAsync($"/api/avatar/items/{itemId}/equipar", null);
        Assert.Equal(HttpStatusCode.OK, equipado.StatusCode);
        Assert.Single((await equipado.Content.ReadFromJsonAsync<AvatarDto>())!.ItemsEquipados);
        using var repetido = await cliente.PostAsync($"/api/avatar/items/{itemId}/equipar", null);
        Assert.Equal(HttpStatusCode.OK, repetido.StatusCode);
        Assert.Single((await repetido.Content.ReadFromJsonAsync<AvatarDto>())!.ItemsEquipados);
        using var ranuraOcupada = await cliente.PostAsync($"/api/avatar/items/{otroId}/equipar", null);
        Assert.Equal(HttpStatusCode.Conflict, ranuraOcupada.StatusCode);

        using var quitar = await cliente.DeleteAsync($"/api/avatar/items/{itemId}/desequipar");
        Assert.Equal(HttpStatusCode.NoContent, quitar.StatusCode);
        using var quitarOtraVez = await cliente.DeleteAsync($"/api/avatar/items/{itemId}/desequipar");
        Assert.Equal(HttpStatusCode.NoContent, quitarOtraVez.StatusCode);
        using var equiparOtro = await cliente.PostAsync($"/api/avatar/items/{otroId}/equipar", null);
        Assert.Equal(HttpStatusCode.OK, equiparOtro.StatusCode);
        Assert.Equal(otroId, Assert.Single((await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!.ItemsEquipados).ItemDeAvatarId);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.SegundoUsuarioId.ToString()));
        Assert.Empty((await cliente.GetFromJsonAsync<ItemInventarioDto[]>("/api/inventario"))!);
        Assert.Empty((await cliente.GetFromJsonAsync<AvatarDto>("/api/avatar"))!.ItemsEquipados);
        using var ajeno = await cliente.PostAsync($"/api/avatar/items/{itemId}/equipar", null);
        Assert.Equal(HttpStatusCode.Conflict, ajeno.StatusCode);
    }
}
