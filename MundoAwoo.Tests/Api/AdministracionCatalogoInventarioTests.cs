using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Application.DTOs.Avatares;
using MundoAwoo.Application.DTOs.Catalogo;
using MundoAwoo.Application.DTOs.Inventario;
using MundoAwoo.Infrastructure.Persistence;
using static MundoAwoo.Tests.TestData;

namespace MundoAwoo.Tests.Api;

public class AdministracionCatalogoInventarioTests
{
    [Fact]
    public async Task RegistroPublico_NoPermiteSolicitarAccesoAdministrador()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();

        using var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/registro", new
        {
            correo = "intento-admin@example.com", contrasena = "Segura!123456aA",
            nombreVisible = "Intento", esAdministrador = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        Assert.False(await db.Usuarios.AnyAsync(usuario => usuario.Correo == "intento-admin@example.com"));
    }

    [Fact]
    public async Task RutasAdministrativas_ExigenCuentaAdministradoraActual()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using var cliente = api.CrearCliente();
        var rutas = new[]
        {
            ("/api/admin/catalogo/categorias", (object)new { nombre = "Ropa", tipo = "Ropa" }),
            ("/api/admin/catalogo/items", (object)new { nombre = "Camisa", categoriaId = Guid.NewGuid(), ranura = "Torso", recursoVisual = "camisa.svg" }),
            ($"/api/inventario/items/{Guid.NewGuid()}/desbloquear", (object)new { usuarioId = api.SegundoUsuarioId })
        };
        foreach (var (ruta, datos) in rutas)
        {
            using var sinToken = await cliente.PostAsJsonAsync(ruta, datos);
            Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                api.CrearToken(api.SegundoUsuarioId.ToString()));
            using var usuarioNormal = await cliente.PostAsJsonAsync(ruta, datos);
            Assert.Equal(HttpStatusCode.Forbidden, usuarioNormal.StatusCode);
            cliente.DefaultRequestHeaders.Authorization = null;
        }

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            var admin = await db.Usuarios.SingleAsync(usuario => usuario.Id == api.PrimerUsuarioId);
            admin.EsAdministrador = true;
            await db.SaveChangesAsync();
        }
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var permitido = await cliente.PostAsJsonAsync("/api/admin/catalogo/categorias",
            new { nombre = "Ropa", tipo = "Ropa" });
        Assert.Equal(HttpStatusCode.Created, permitido.StatusCode);

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            var admin = await db.Usuarios.SingleAsync(usuario => usuario.Id == api.PrimerUsuarioId);
            admin.EsAdministrador = false;
            await db.SaveChangesAsync();
        }
        using var revocado = await cliente.PostAsJsonAsync("/api/admin/catalogo/categorias",
            new { nombre = "Accesorios", tipo = "Accesorio" });
        Assert.Equal(HttpStatusCode.Forbidden, revocado.StatusCode);
    }

    [Fact]
    public async Task Admin_CreaCatalogoConValidacionesYConcedeItemQueSePuedeEquipar()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            var admin = await db.Usuarios.SingleAsync(usuario => usuario.Id == api.PrimerUsuarioId);
            admin.EsAdministrador = true;
            db.Avatares.Add(CrearAvatar(api.SegundoUsuarioId));
            await db.SaveChangesAsync();
        }
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));

        using var categoriaRespuesta = await cliente.PostAsJsonAsync("/api/admin/catalogo/categorias",
            new { nombre = "  Ropa  ", tipo = "ropa" });
        Assert.Equal(HttpStatusCode.Created, categoriaRespuesta.StatusCode);
        CategoriaDto categoria = (await categoriaRespuesta.Content.ReadFromJsonAsync<CategoriaDto>())!;
        Assert.Equal("Ropa", categoria.Nombre);
        Assert.Equal("Ropa", categoria.Tipo);
        using var duplicada = await cliente.PostAsJsonAsync("/api/admin/catalogo/categorias",
            new { nombre = "ROPA", tipo = "Ropa" });
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
        using var tipoInvalido = await cliente.PostAsJsonAsync("/api/admin/catalogo/categorias",
            new { nombre = "Otra", tipo = "123" });
        Assert.Equal(HttpStatusCode.BadRequest, tipoInvalido.StatusCode);

        using var categoriaInexistente = await cliente.PostAsJsonAsync("/api/admin/catalogo/items",
            new { nombre = "Camisa", categoriaId = Guid.NewGuid(), ranura = "Torso", recursoVisual = "camisa.svg" });
        Assert.Equal(HttpStatusCode.NotFound, categoriaInexistente.StatusCode);
        using var ranuraIncompatible = await cliente.PostAsJsonAsync("/api/admin/catalogo/items",
            new { nombre = "Camisa", categoriaId = categoria.Id, ranura = "Cabeza", recursoVisual = "camisa.svg" });
        Assert.Equal(HttpStatusCode.BadRequest, ranuraIncompatible.StatusCode);
        using var itemRespuesta = await cliente.PostAsJsonAsync("/api/admin/catalogo/items",
            new { nombre = "  Camisa  ", categoriaId = categoria.Id, ranura = "torso", recursoVisual = " camisa.svg " });
        Assert.Equal(HttpStatusCode.Created, itemRespuesta.StatusCode);
        ItemCatalogoDto item = (await itemRespuesta.Content.ReadFromJsonAsync<ItemCatalogoDto>())!;
        Assert.Equal("Camisa", item.Nombre);
        Assert.Equal("camisa.svg", item.RecursoVisual);
        using var itemDuplicado = await cliente.PostAsJsonAsync("/api/admin/catalogo/items",
            new { nombre = "CAMISA", categoriaId = categoria.Id, ranura = "Torso", recursoVisual = "otra.svg" });
        Assert.Equal(HttpStatusCode.Conflict, itemDuplicado.StatusCode);

        using var concesion = await cliente.PostAsJsonAsync($"/api/inventario/items/{item.Id}/desbloquear",
            new { usuarioId = api.SegundoUsuarioId });
        Assert.Equal(HttpStatusCode.OK, concesion.StatusCode);
        ItemInventarioDto entrada = (await concesion.Content.ReadFromJsonAsync<ItemInventarioDto>())!;
        Assert.Equal(item.Id, entrada.Item.Id);
        using var repetida = await cliente.PostAsJsonAsync($"/api/inventario/items/{item.Id}/desbloquear",
            new { usuarioId = api.SegundoUsuarioId });
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        Assert.Equal(entrada.FechaDesbloqueo,
            (await repetida.Content.ReadFromJsonAsync<ItemInventarioDto>())!.FechaDesbloqueo);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.SegundoUsuarioId.ToString()));
        var inventario = (await cliente.GetFromJsonAsync<ItemInventarioDto[]>("/api/inventario"))!;
        Assert.Single(inventario);
        using var equipado = await cliente.PostAsync($"/api/avatar/items/{item.Id}/equipar", null);
        Assert.Equal(HttpStatusCode.OK, equipado.StatusCode);
        Assert.Equal(item.Id, Assert.Single((await equipado.Content.ReadFromJsonAsync<AvatarDto>())!.ItemsEquipados).ItemDeAvatarId);
    }

    [Fact]
    public async Task Concesion_ValidaUsuarioEItemSinCrearInventarioParcial()
    {
        using var api = new PerfilApiFactory();
        await api.PrepararDatosAsync();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
            var admin = await db.Usuarios.SingleAsync(usuario => usuario.Id == api.PrimerUsuarioId);
            admin.EsAdministrador = true;
            await db.SaveChangesAsync();
        }
        using var cliente = api.CrearCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.CrearToken(api.PrimerUsuarioId.ToString()));
        using var usuarioInexistente = await cliente.PostAsJsonAsync(
            $"/api/inventario/items/{Guid.NewGuid()}/desbloquear", new { usuarioId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, usuarioInexistente.StatusCode);
        using var itemInexistente = await cliente.PostAsJsonAsync(
            $"/api/inventario/items/{Guid.NewGuid()}/desbloquear", new { usuarioId = api.SegundoUsuarioId });
        Assert.Equal(HttpStatusCode.NotFound, itemInexistente.StatusCode);
        using var datosInvalidos = await cliente.PostAsJsonAsync(
            $"/api/inventario/items/{Guid.Empty}/desbloquear", new { usuarioId = api.SegundoUsuarioId });
        Assert.Equal(HttpStatusCode.BadRequest, datosInvalidos.StatusCode);
        using var scopeFinal = api.Services.CreateScope();
        var contexto = scopeFinal.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        Assert.Empty(await contexto.ItemsDesbloqueados.ToListAsync());
    }
}
