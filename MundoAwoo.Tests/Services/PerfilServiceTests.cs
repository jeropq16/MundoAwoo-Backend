using static MundoAwoo.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using MundoAwoo.Application.DTOs.Perfiles;
using MundoAwoo.Application.Exceptions;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Application.Services;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.Interfaces;
using MundoAwoo.Infrastructure.Persistence;
using MundoAwoo.Infrastructure.Persistence.Repositories;

namespace MundoAwoo.Tests.Services;

public class PerfilServiceTests
{
    [Fact]
    public async Task Obtener_DevuelveUnaCopiaDelPerfilSinGuardar()
    {
        var usuario = CrearUsuario("Fan");
        ActualizarPerfil(usuario, "Fan", "Mi biografía");
        var repositorio = new UsuarioRepositoryPrueba(usuario);
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(repositorio, unidad, new UsuarioService());

        PerfilUsuarioDto resultado = await servicio.ObtenerAsync(usuario.Id);

        Assert.Equal(usuario.Perfil.Id, resultado.Id);
        Assert.Equal(usuario.Id, resultado.UsuarioId);
        Assert.Equal("Fan", resultado.NombreVisible);
        Assert.Equal("Mi biografía", resultado.Biografia);
        Assert.Equal(0, unidad.Guardados);
        resultado.NombreVisible = "Cambio solo en el DTO";
        Assert.Equal("Fan", usuario.Perfil.NombreVisible);
    }

    [Fact]
    public async Task Actualizar_NormalizaLosDatosYConfirmaUnaSolaVez()
    {
        var usuario = CrearUsuario("Fan");
        var repositorio = new UsuarioRepositoryPrueba(usuario);
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(repositorio, unidad, new UsuarioService());
        using var cancelacion = new CancellationTokenSource();

        var resultado = await servicio.ActualizarAsync(usuario.Id, new ActualizarPerfilDto
        {
            NombreVisible = "  Nuevo nombre  ",
            Biografia = "  Nueva biografía  "
        }, cancelacion.Token);

        Assert.Equal("Nuevo nombre", resultado.NombreVisible);
        Assert.Equal("Nueva biografía", resultado.Biografia);
        Assert.Equal(usuario.Perfil.Id, resultado.Id);
        Assert.Equal(usuario.Id, resultado.UsuarioId);
        Assert.Equal(1, repositorio.Consultas);
        Assert.Equal(1, unidad.Guardados);
        Assert.Equal(cancelacion.Token, repositorio.UltimoToken);
        Assert.Equal(cancelacion.Token, unidad.UltimoToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UsuarioInexistente_LanzaErrorSinGuardar(bool actualizar)
    {
        var repositorio = new UsuarioRepositoryPrueba(null);
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(repositorio, unidad, new UsuarioService());

        await Assert.ThrowsAsync<UsuarioNoEncontradoException>(() => actualizar
            ? servicio.ActualizarAsync(Guid.NewGuid(), new ActualizarPerfilDto { NombreVisible = "Fan" })
            : servicio.ObtenerAsync(Guid.NewGuid()));

        Assert.Equal(0, unidad.Guardados);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdentificadorVacio_SeRechazaAntesDeConsultar(bool actualizar)
    {
        var repositorio = new UsuarioRepositoryPrueba(null);
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(repositorio, unidad, new UsuarioService());

        await Assert.ThrowsAsync<ArgumentException>(() => actualizar
            ? servicio.ActualizarAsync(Guid.Empty, new ActualizarPerfilDto { NombreVisible = "Fan" })
            : servicio.ObtenerAsync(Guid.Empty));

        Assert.Equal(0, repositorio.Consultas);
        Assert.Equal(0, unidad.Guardados);
    }

    [Fact]
    public async Task Actualizar_SinDto_NoConsultaNiGuarda()
    {
        var repositorio = new UsuarioRepositoryPrueba(null);
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(repositorio, unidad, new UsuarioService());

        await Assert.ThrowsAsync<ArgumentNullException>(() => servicio.ActualizarAsync(Guid.NewGuid(), null!));

        Assert.Equal(0, repositorio.Consultas);
        Assert.Equal(0, unidad.Guardados);
    }

    [Theory]
    [InlineData(null, "Biografía")]
    [InlineData("", "Biografía")]
    [InlineData("   ", "Biografía")]
    [InlineData("Nuevo nombre", null)]
    public async Task Actualizar_TextoInvalido_NoModificaElPerfilNiGuarda(string? nombre, string? biografia)
    {
        var usuario = CrearUsuario("Nombre original");
        ActualizarPerfil(usuario, "Nombre original", "Biografía original");
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(new UsuarioRepositoryPrueba(usuario), unidad, new UsuarioService());

        await Assert.ThrowsAnyAsync<ArgumentException>(() => servicio.ActualizarAsync(usuario.Id,
            new ActualizarPerfilDto { NombreVisible = nombre!, Biografia = biografia! }));

        Assert.Equal("Nombre original", usuario.Perfil.NombreVisible);
        Assert.Equal("Biografía original", usuario.Perfil.Biografia);
        Assert.Equal(0, unidad.Guardados);
    }

    [Theory]
    [InlineData(51, 0)]
    [InlineData(5, 501)]
    public async Task Actualizar_TextoDemasiadoLargo_NoConfirmaCambios(int longitudNombre, int longitudBiografia)
    {
        var usuario = CrearUsuario("Fan");
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(new UsuarioRepositoryPrueba(usuario), unidad, new UsuarioService());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ActualizarAsync(usuario.Id,
            new ActualizarPerfilDto
            {
                NombreVisible = new string('n', longitudNombre),
                Biografia = new string('b', longitudBiografia)
            }));

        Assert.Equal("Fan", usuario.Perfil.NombreVisible);
        Assert.Equal(0, unidad.Guardados);
    }

    [Fact]
    public async Task Actualizar_SiFallaElGuardado_PropagaElError()
    {
        var usuario = CrearUsuario("Fan");
        var error = new InvalidOperationException("Fallo de persistencia simulado.");
        var unidad = new UnitOfWorkPrueba { ErrorAlGuardar = error };
        var servicio = new PerfilService(new UsuarioRepositoryPrueba(usuario), unidad, new UsuarioService());

        var recibido = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ActualizarAsync(
            usuario.Id, new ActualizarPerfilDto { NombreVisible = "Nuevo nombre" }));

        Assert.Same(error, recibido);
        Assert.Equal(1, unidad.Guardados);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OperacionCancelada_NoConsultaNiModifica(bool actualizar)
    {
        var usuario = CrearUsuario("Fan");
        var repositorio = new UsuarioRepositoryPrueba(usuario);
        var unidad = new UnitOfWorkPrueba();
        var servicio = new PerfilService(repositorio, unidad, new UsuarioService());
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => actualizar
            ? servicio.ActualizarAsync(usuario.Id, new ActualizarPerfilDto { NombreVisible = "Nuevo" }, cancelacion.Token)
            : servicio.ObtenerAsync(usuario.Id, cancelacion.Token));

        Assert.Equal("Fan", usuario.Perfil.NombreVisible);
        Assert.Equal(0, repositorio.Consultas);
        Assert.Equal(0, unidad.Guardados);
    }

    [Fact]
    public async Task ServicioConRepositorioReal_GuardaSoloElPerfilSolicitado()
    {
        // SQLite temporal comprueba el flujo completo sin un servidor MySQL.
        var opciones = new DbContextOptionsBuilder<AwoorldDbContext>()
            .UseSqlite("Data Source=:memory:;Foreign Keys=True")
            .Options;
        await using var contexto = new AwoorldDbContext(opciones);
        await contexto.Database.OpenConnectionAsync();
        await contexto.Database.EnsureCreatedAsync();
        var usuarios = new UsuarioRepository(contexto);
        var unidad = new UnitOfWork(contexto);
        var primero = CrearUsuario("Primero");
        var segundo = CrearUsuario("Segundo");
        usuarios.Agregar(primero);
        usuarios.Agregar(segundo);
        await unidad.GuardarCambiosAsync();
        contexto.ChangeTracker.Clear();
        var servicio = new PerfilService(usuarios, unidad, new UsuarioService());

        await servicio.ActualizarAsync(primero.Id, new ActualizarPerfilDto
        {
            NombreVisible = "  Actualizado  ",
            Biografia = "  Mi biografía  "
        });
        contexto.ChangeTracker.Clear();

        var guardado = await servicio.ObtenerAsync(primero.Id);
        var sinCambios = await servicio.ObtenerAsync(segundo.Id);
        Assert.Equal("Actualizado", guardado.NombreVisible);
        Assert.Equal("Mi biografía", guardado.Biografia);
        Assert.Equal("Segundo", sinCambios.NombreVisible);

        // La biografía vacía es válida y permite borrar el texto anterior.
        await servicio.ActualizarAsync(primero.Id, new ActualizarPerfilDto { NombreVisible = "Actualizado" });
        contexto.ChangeTracker.Clear();
        Assert.Empty((await servicio.ObtenerAsync(primero.Id)).Biografia);
    }

    // Dobles de prueba sencillos: implementan los contratos sin acceder a una base.
    // Permiten verificar cuándo el servicio consulta y cuándo solicita guardar.
    private class UsuarioRepositoryPrueba : IUsuarioRepository
    {
        private readonly Usuario? _usuario;
        public int Consultas { get; private set; }
        public CancellationToken UltimoToken { get; private set; }

        public UsuarioRepositoryPrueba(Usuario? usuario)
        {
            _usuario = usuario;
        }

        public Task<Usuario?> ObtenerPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Consultas++;
            UltimoToken = cancellationToken;
            return Task.FromResult(_usuario?.Id == usuarioId ? _usuario : null);
        }

        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("El servicio de perfil no consulta correos.");
        }

        public Task<bool> EsAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("El servicio de perfil no consulta administradores.");
        }

        public void Agregar(Usuario usuario)
        {
            throw new NotSupportedException("El servicio de perfil no debe crear usuarios.");
        }
    }

    private class UnitOfWorkPrueba : IUnitOfWork
    {
        public int Guardados { get; private set; }
        public CancellationToken UltimoToken { get; private set; }
        public Exception? ErrorAlGuardar { get; set; }

        public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guardados++;
            UltimoToken = cancellationToken;
            if (ErrorAlGuardar != null)
                throw ErrorAlGuardar;

            return Task.FromResult(1);
        }
    }
}
