using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.ValueObjects;
using MundoAwoo.Infrastructure.Persistence;

namespace MundoAwoo.Tests.Persistence;

// Estas pruebas validan el modelo y el SQL del proveedor real sin abrir una conexión.
// No sustituyen una prueba de guardar y consultar contra un servidor MySQL instalado.
public class ModeloMySqlTests
{
    [Fact]
    public void Modelo_ProtegeLasRelacionesUnoAUnoYLosDuplicados()
    {
        using var contexto = CrearContexto();

        ComprobarIndiceUnico<Avatar>(contexto, "UsuarioId");
        ComprobarIndiceUnico<PerfilUsuario>(contexto, "UsuarioId");
        ComprobarIndiceUnico<ItemDesbloqueado>(contexto, "UsuarioId", "ItemDeAvatarId");
        ComprobarIndiceUnico<ItemEquipado>(contexto, "AvatarId", "Ranura");
        ComprobarIndiceUnico<ItemEquipado>(contexto, "AvatarId", "ItemDeAvatarId");
    }

    [Fact]
    public void Modelo_ConservaLasNavegacionesSinCrearClavesForaneasOcultas()
    {
        using var contexto = CrearContexto();
        var usuario = contexto.Model.FindEntityType(typeof(Usuario))!;
        var avatar = contexto.Model.FindEntityType(typeof(Avatar))!;

        Assert.NotNull(usuario.FindNavigation(nameof(Usuario.ItemsDesbloqueados)));
        Assert.NotNull(avatar.FindNavigation(nameof(Avatar.ItemsEquipados)));

        // Una FK oculta como UsuarioId1 indicaría una relación configurada dos veces por error.
        Assert.All(contexto.Model.GetEntityTypes().SelectMany(entidad => entidad.GetForeignKeys()),
            relacion => Assert.All(relacion.Properties, propiedad => Assert.False(propiedad.IsShadowProperty())));
    }

    [Fact]
    public void Conversiones_ConservanColorYElInstanteUtc()
    {
        using var contexto = CrearContexto();
        var color = contexto.Model.FindEntityType(typeof(Avatar))!
            .FindProperty(nameof(Avatar.ColorFondo))!.GetValueConverter()!;
        Assert.Equal("#AABBCC", color.ConvertToProvider(new ColorHex("#aabbcc")));
        Assert.Equal(new ColorHex("#AABBCC"), color.ConvertFromProvider("#AABBCC"));

        var fecha = contexto.Model.FindEntityType(typeof(Usuario))!
            .FindProperty(nameof(Usuario.FechaRegistro))!.GetValueConverter()!;
        var horaColombia = new DateTimeOffset(2026, 10, 27, 10, 0, 0, TimeSpan.FromHours(-5));
        var almacenada = Assert.IsType<DateTime>(fecha.ConvertToProvider(horaColombia));
        // MySQL no devuelve DateTimeKind: el conversor debe restaurar UTC explícitamente.
        var recuperada = Assert.IsType<DateTimeOffset>(fecha.ConvertFromProvider(DateTime.SpecifyKind(almacenada, DateTimeKind.Unspecified)));

        Assert.Equal(horaColombia, recuperada);
        Assert.Equal(TimeSpan.Zero, recuperada.Offset);
        Assert.Equal(15, recuperada.Hour);
    }

    [Fact]
    public void ConsultasDeAgregados_SeTraducenConElProveedorMySql()
    {
        using var contexto = CrearContexto();

        string usuarioSql = contexto.Usuarios.Include(usuario => usuario.Perfil)
            .Include(usuario => usuario.ItemsDesbloqueados).ToQueryString();
        string avatarSql = contexto.Avatares.Include(avatar => avatar.ItemsEquipados).ToQueryString();

        Assert.Contains("`PerfilesUsuario`", usuarioSql);
        Assert.Contains("`ItemsDesbloqueados`", usuarioSql);
        Assert.Contains("`ItemsEquipados`", avatarSql);
    }

    [Fact]
    public void Borrado_DelCatalogoEsRestringidoYElDelPropietarioEsEnCascada()
    {
        using var contexto = CrearContexto();

        Assert.All(contexto.Model.GetEntityTypes().SelectMany(entidad => entidad.GetForeignKeys()), relacion =>
        {
            Type principal = relacion.PrincipalEntityType.ClrType;
            if (principal == typeof(Categoria) || principal == typeof(ItemDeAvatar))
                Assert.Equal(DeleteBehavior.Restrict, relacion.DeleteBehavior);
            else
                Assert.Equal(DeleteBehavior.Cascade, relacion.DeleteBehavior);
        });
    }

    private static void ComprobarIndiceUnico<T>(AwoorldDbContext contexto, params string[] propiedades)
    {
        var entidad = contexto.Model.FindEntityType(typeof(T))!;
        Assert.Contains(entidad.GetIndexes(), indice =>
            indice.IsUnique && indice.Properties.Select(propiedad => propiedad.Name).SequenceEqual(propiedades));
    }

    private static AwoorldDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AwoorldDbContext>()
            .UseMySql("Server=127.0.0.1;Database=awoorld_model_tests;User ID=unused;Password=unused;",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;
        return new AwoorldDbContext(options);
    }
}
