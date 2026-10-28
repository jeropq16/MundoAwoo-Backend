using Microsoft.EntityFrameworkCore;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Enums;
using MundoAwoo.Domain.ValueObjects;
using MundoAwoo.Infrastructure.Persistence;
using MundoAwoo.Infrastructure.Persistence.Repositories;

namespace MundoAwoo.Tests.Persistence;

// SQLite temporal prueba consultas y persistencia reales sin instalar un servidor.
// No ejecuta las migraciones MySQL ni sustituye las pruebas contra ese motor.
public class RepositoriosTests
{
    [Fact]
    public async Task Agregar_DejaLosAgregadosPendientesHastaConfirmarLaUnidadDeTrabajo()
    {
        await using var contexto = await CrearContextoAsync();
        var usuarios = new UsuarioRepository(contexto);
        var avatares = new AvatarRepository(contexto);
        var unidad = new UnitOfWork(contexto);
        var usuario = new Usuario("Fan");
        var avatar = new Avatar(usuario.Id);

        usuarios.Agregar(usuario);
        avatares.Agregar(avatar);

        // Estas consultas leen la base, no los objetos pendientes del contexto.
        Assert.Equal(0, await contexto.Usuarios.CountAsync());
        Assert.Equal(0, await contexto.Avatares.CountAsync());
        await unidad.GuardarCambiosAsync();

        // Evitamos que EF nos devuelva los objetos que acabamos de crear en memoria.
        contexto.ChangeTracker.Clear();
        var recuperado = await usuarios.ObtenerPorIdAsync(usuario.Id);
        var avatarRecuperado = await avatares.ObtenerPorUsuarioIdAsync(usuario.Id);

        Assert.NotNull(recuperado);
        Assert.Equal("Fan", recuperado.Perfil.NombreVisible);
        Assert.Equal(usuario.Perfil.Id, recuperado.Perfil.Id);
        Assert.Empty(recuperado.ItemsDesbloqueados);
        Assert.NotNull(avatarRecuperado);
        Assert.Equal(avatar.Id, avatarRecuperado.Id);
        Assert.Equal(0, await unidad.GuardarCambiosAsync());
    }

    [Fact]
    public async Task ConsultarAgregados_CargaRelacionesYPermiteGuardarLosCambiosDelDominio()
    {
        await using var contexto = await CrearContextoAsync();
        var usuarios = new UsuarioRepository(contexto);
        var avatares = new AvatarRepository(contexto);
        var catalogo = new ItemDeAvatarRepository(contexto);
        var unidad = new UnitOfWork(contexto);
        var categoria = new Categoria("Ropa", TipoCategoria.Ropa);
        var camiseta = new ItemDeAvatar("Camiseta", categoria, RanuraAvatar.Torso, "camiseta.png");
        // Preparación del catálogo: todavía no existe un caso de uso administrativo.
        contexto.Categorias.Add(categoria);
        contexto.ItemsDeAvatar.Add(camiseta);
        var usuario = new Usuario("Fan");
        usuario.DesbloquearItem(camiseta);
        usuarios.Agregar(usuario);
        avatares.Agregar(new Avatar(usuario.Id));
        var otroUsuario = new Usuario("Otro fan");
        usuarios.Agregar(otroUsuario);
        avatares.Agregar(new Avatar(otroUsuario.Id));
        await unidad.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        // Los repositorios de agregados deben mantener su contrato incluso si cambia el default.
        contexto.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var recuperado = await usuarios.ObtenerPorIdAsync(usuario.Id);
        var avatar = await avatares.ObtenerPorUsuarioIdAsync(usuario.Id);
        var item = await catalogo.ObtenerPorIdAsync(camiseta.Id);
        Assert.NotNull(recuperado);
        Assert.NotNull(avatar);
        Assert.NotNull(item);
        Assert.True(recuperado.TieneItemDesbloqueado(item.Id));
        Assert.Equal(usuario.Id, avatar.UsuarioId);

        recuperado.ActualizarPerfil("Nuevo nombre", "Biografía actualizada");
        avatar.Equipar(item, recuperado);
        avatar.CambiarColorFondo(new ColorHex("#AABBCC"));
        await unidad.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();

        var perfilGuardado = await usuarios.ObtenerPorIdAsync(usuario.Id);
        var avatarGuardado = await avatares.ObtenerPorUsuarioIdAsync(usuario.Id);
        Assert.NotNull(perfilGuardado);
        Assert.NotNull(avatarGuardado);
        Assert.Equal("Nuevo nombre", perfilGuardado.Perfil.NombreVisible);
        Assert.Equal("Biografía actualizada", perfilGuardado.Perfil.Biografia);
        Assert.Equal(new ColorHex("#AABBCC"), avatarGuardado.ColorFondo);
        Assert.Equal(item.Id, Assert.Single(avatarGuardado.ItemsEquipados).ItemDeAvatarId);
        Assert.Equal(1, await contexto.ItemsDeAvatar.CountAsync());

        // Quitar de la lista privada debe eliminar el equipamiento persistido, no el desbloqueo.
        avatarGuardado.Desequipar(item.Id);
        await unidad.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();
        var sinEquipamiento = await avatares.ObtenerPorUsuarioIdAsync(usuario.Id);
        Assert.NotNull(sinEquipamiento);
        Assert.Empty(sinEquipamiento.ItemsEquipados);
        Assert.Equal(0, await contexto.ItemsEquipados.CountAsync());
        Assert.Equal(1, await contexto.ItemsDesbloqueados.CountAsync());
        var otroPerfil = await usuarios.ObtenerPorIdAsync(otroUsuario.Id);
        Assert.Equal("Otro fan", otroPerfil!.Perfil.NombreVisible);
    }

    [Fact]
    public async Task Catalogo_FiltraOrdenaYNoDejaEntidadesEnSeguimiento()
    {
        await using var contexto = await CrearContextoAsync();
        var ropa = new Categoria("Ropa", TipoCategoria.Ropa);
        var accesorios = new Categoria("Accesorios", TipoCategoria.Accesorio);
        var camiseta = new ItemDeAvatar("Camiseta", ropa, RanuraAvatar.Torso, "camiseta.png");
        var buzo = new ItemDeAvatar("Buzo", ropa, RanuraAvatar.Torso, "buzo.png");
        var lentes = new ItemDeAvatar("Lentes", accesorios, RanuraAvatar.Ojos, "lentes.png");
        contexto.Categorias.AddRange(ropa, accesorios);
        contexto.ItemsDeAvatar.AddRange(camiseta, buzo, lentes);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var catalogo = new ItemDeAvatarRepository(contexto);

        var items = await catalogo.ObtenerPorCategoriaAsync(ropa.Id);
        var categorias = await catalogo.ObtenerCategoriasAsync();
        var itemPorId = await catalogo.ObtenerPorIdAsync(lentes.Id);

        Assert.Equal(new[] { buzo.Id, camiseta.Id }, items.Select(item => item.Id));
        Assert.Equal(new[] { accesorios.Id, ropa.Id }, categorias.Select(categoria => categoria.Id));
        Assert.NotNull(itemPorId);
        Assert.Equal("Lentes", itemPorId.Nombre);
        Assert.Empty(contexto.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ConsultarDatosInexistentes_DevuelveNullOListasVacias()
    {
        await using var contexto = await CrearContextoAsync();
        Guid inexistente = Guid.NewGuid();
        var catalogo = new ItemDeAvatarRepository(contexto);

        Assert.Null(await new UsuarioRepository(contexto).ObtenerPorIdAsync(inexistente));
        Assert.Null(await new AvatarRepository(contexto).ObtenerPorUsuarioIdAsync(inexistente));
        Assert.Null(await catalogo.ObtenerPorIdAsync(inexistente));
        Assert.Empty(await catalogo.ObtenerPorCategoriaAsync(inexistente));
        Assert.Empty(await catalogo.ObtenerCategoriasAsync());
    }

    [Fact]
    public async Task Guardar_ConUnaReferenciaInvalida_NoDejaUnUsuarioParcialmenteCreado()
    {
        await using var contexto = await CrearContextoAsync();
        var usuario = new Usuario("Fan");
        var itemNoGuardado = new ItemDeAvatar("Camiseta", new Categoria("Ropa", TipoCategoria.Ropa),
            RanuraAvatar.Torso, "camiseta.png");
        usuario.DesbloquearItem(itemNoGuardado);
        new UsuarioRepository(contexto).Agregar(usuario);
        new AvatarRepository(contexto).Agregar(new Avatar(usuario.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => new UnitOfWork(contexto).GuardarCambiosAsync());
        contexto.ChangeTracker.Clear();

        Assert.Equal(0, await contexto.Usuarios.CountAsync());
        Assert.Equal(0, await contexto.PerfilesUsuario.CountAsync());
        Assert.Equal(0, await contexto.Avatares.CountAsync());
    }

    [Fact]
    public async Task ConsultasYGuardado_RespetanLaCancelacion()
    {
        await using var contexto = await CrearContextoAsync();
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();
        Guid id = Guid.NewGuid();
        var catalogo = new ItemDeAvatarRepository(contexto);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UsuarioRepository(contexto).ObtenerPorIdAsync(id, cancelacion.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new AvatarRepository(contexto).ObtenerPorUsuarioIdAsync(id, cancelacion.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => catalogo.ObtenerPorIdAsync(id, cancelacion.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => catalogo.ObtenerPorCategoriaAsync(id, cancelacion.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => catalogo.ObtenerCategoriasAsync(cancelacion.Token));

        new UsuarioRepository(contexto).Agregar(new Usuario("Fan"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UnitOfWork(contexto).GuardarCambiosAsync(cancelacion.Token));
        Assert.Equal(0, await contexto.Usuarios.CountAsync());
    }

    private static async Task<AwoorldDbContext> CrearContextoAsync()
    {
        var options = new DbContextOptionsBuilder<AwoorldDbContext>()
            .UseSqlite("Data Source=:memory:;Foreign Keys=True")
            .Options;
        var contexto = new AwoorldDbContext(options);
        // Mantener abierta la conexión conserva la base temporal hasta desechar el contexto.
        await contexto.Database.OpenConnectionAsync();
        // Solo para esta base desechable de pruebas; MySQL continúa usando migraciones.
        await contexto.Database.EnsureCreatedAsync();
        return contexto;
    }
}
