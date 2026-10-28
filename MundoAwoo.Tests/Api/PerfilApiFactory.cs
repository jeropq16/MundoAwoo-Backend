using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using MundoAwoo.Application.DTOs.Perfiles;
using MundoAwoo.Application.DTOs.Autenticacion;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Domain.Entities;
using MundoAwoo.Infrastructure.Persistence;

namespace MundoAwoo.Tests.Api;

// Inicia la API real en memoria: conserva JWT, autorización, controladores y middleware.
// Únicamente sustituye MySQL por una conexión SQLite temporal para aislar los datos.
public class PerfilApiFactory : WebApplicationFactory<Program>
{
    // Exclusiva de pruebas; nunca debe copiarse a la configuración de una API real.
    private const string ClavePruebas = "Solo-para-pruebas-HTTP-Awoorld-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ-abcdef";
    private const string Emisor = "Awoorld.Tests";
    private const string Destinatario = "Awoorld.Tests.Api";
    private readonly SqliteConnection _conexion = new("Data Source=:memory:;Foreign Keys=True");
    private readonly bool _simularFallo;
    private readonly bool _simularFalloToken;

    public Guid PrimerUsuarioId { get; private set; }
    public Guid SegundoUsuarioId { get; private set; }

    public PerfilApiFactory(bool simularFallo = false, bool simularFalloToken = false)
    {
        _simularFallo = simularFallo;
        _simularFalloToken = simularFalloToken;
        _conexion.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Awoorld", "Server=127.0.0.1;Database=unused;User ID=unused;");
        builder.UseSetting("Jwt:Issuer", Emisor);
        builder.UseSetting("Jwt:Audience", Destinatario);
        builder.UseSetting("Jwt:SigningKey", ClavePruebas);
        builder.UseSetting("Jwt:DuracionMinutos", "15");
        builder.ConfigureServices(services =>
        {
            // Quitamos también la configuración del proveedor anterior para no mezclar motores.
            services.RemoveAll<AwoorldDbContext>();
            services.RemoveAll<DbContextOptions<AwoorldDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AwoorldDbContext>>();
            services.AddDbContext<AwoorldDbContext>(options => options.UseSqlite(_conexion));

            if (_simularFalloToken)
            {
                services.RemoveAll<IJwtTokenService>();
                services.AddScoped<IJwtTokenService, TokenConFallo>();
            }

            if (_simularFallo)
            {
                services.RemoveAll<IPerfilService>();
                services.AddScoped<IPerfilService, PerfilConFallo>();
            }
        });
    }

    public async Task PrepararDatosAsync()
    {
        using var scope = Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        await contexto.Database.EnsureCreatedAsync();
        var primero = new Usuario("Primer fan");
        var segundo = new Usuario("Segundo fan");
        contexto.Usuarios.AddRange(primero, segundo);
        await contexto.SaveChangesAsync();
        PrimerUsuarioId = primero.Id;
        SegundoUsuarioId = segundo.Id;
    }

    public HttpClient CrearCliente()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    public string CrearToken(string? sujeto, string caso = "valido")
    {
        var claims = new List<Claim>();
        if (sujeto != null)
            claims.Add(new Claim("sub", sujeto));
        if (caso == "duplicado")
            claims.Add(new Claim("sub", SegundoUsuarioId.ToString()));

        string clave = caso == "firma" ? new string('x', 80) : ClavePruebas;
        string algoritmo = caso == "algoritmo" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256;
        var firma = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)), algoritmo);
        DateTime inicio = DateTime.UtcNow.AddMinutes(-5);
        DateTime? fin = DateTime.UtcNow.AddMinutes(5);
        if (caso == "vencido")
            fin = DateTime.UtcNow.AddMinutes(-2);
        if (caso == "futuro")
        {
            inicio = DateTime.UtcNow.AddHours(1);
            fin = DateTime.UtcNow.AddHours(2);
        }
        if (caso == "sin-expiracion")
            fin = null;

        var token = new JwtSecurityToken(
            issuer: caso == "emisor" ? "Otro.Emisor" : Emisor,
            audience: caso == "destinatario" ? "Otra.Api" : Destinatario,
            claims: claims,
            notBefore: inicio,
            expires: fin,
            signingCredentials: caso == "sin-firma" ? null : firma);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _conexion.Dispose();
    }

    private class PerfilConFallo : IPerfilService
    {
        public Task<PerfilUsuarioDto> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("DETALLE_INTERNO_NO_EXPONER");
        }

        public Task<PerfilUsuarioDto> ActualizarAsync(Guid usuarioId, ActualizarPerfilDto datos,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("DETALLE_INTERNO_NO_EXPONER");
        }
    }

    private class TokenConFallo : IJwtTokenService
    {
        public AutenticacionRespuestaDto Crear(Guid usuarioId)
        {
            throw new InvalidOperationException("Fallo simulado al emitir el token después de guardar Identity.");
        }
    }
}
