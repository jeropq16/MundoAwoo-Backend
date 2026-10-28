using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using MundoAwoo.Api.Middlewares;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Application.Services;
using MundoAwoo.Domain.Interfaces;
using MundoAwoo.Infrastructure.Persistence;
using MundoAwoo.Infrastructure.Persistence.Repositories;
using MundoAwoo.Infrastructure.Identity;
using MundoAwoo.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Un campo desconocido (por ejemplo UsuarioId en el PUT) es un error de solicitud.
    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddProblemDetails();
AutenticacionJwt.Registrar(builder.Services, builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// La conexión real se configura en User Secrets o variables de entorno, fuera del repositorio.
string? conexion = builder.Configuration.GetConnectionString("Awoorld");
if (string.IsNullOrWhiteSpace(conexion))
    throw new InvalidOperationException("Configura ConnectionStrings:Awoorld. Consulta docs/03-mysql.md.");

string versionMySql = builder.Configuration["Database:ServerVersion"] ?? "8.4.0";
builder.Services.AddDbContext<AwoorldDbContext>(options =>
    options.UseMySql(conexion, new MySqlServerVersion(Version.Parse(versionMySql))));
// La versión explícita permite generar migraciones sin conectarse al servidor.
// Aplicaremos las migraciones con dotnet ef database update, no en cada arranque de la API.

// AddIdentityCore aporta UserManager sin cambiar la autenticación Bearer por cookies.
builder.Services.AddIdentityCore<UsuarioIdentity>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    // La verificación de correo tendrá su propio flujo más adelante.
    options.SignIn.RequireConfirmedEmail = false;
    options.Stores.MaxLengthForKeys = 128;
})
    .AddEntityFrameworkStores<AwoorldDbContext>()
    .AddSignInManager();

// Scoped crea una instancia por solicitud HTTP. AddDbContext también usa ese alcance:
// los tres repositorios y la unidad de trabajo comparten el mismo contexto y sus cambios.
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAvatarRepository, AvatarRepository>();
builder.Services.AddScoped<IItemDeAvatarRepository, ItemDeAvatarRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// El servicio de perfil recibirá los repositorios y la unidad de trabajo del mismo alcance.
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();

var app = builder.Build();

// Debe envolver los componentes posteriores para capturar sus excepciones.
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Primero validamos el token y después comprobamos los permisos del endpoint.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite iniciar la API dentro de las pruebas HTTP con WebApplicationFactory.
public partial class Program { }
