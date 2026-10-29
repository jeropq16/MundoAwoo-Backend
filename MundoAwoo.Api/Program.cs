using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MundoAwoo.Api.Middlewares;
using MundoAwoo.Api.Security;
using MundoAwoo.Application.Interfaces;
using MundoAwoo.Application.Services;
using MundoAwoo.Domain.Interfaces;
using MundoAwoo.Infrastructure.Persistence;
using MundoAwoo.Infrastructure.Persistence.Repositories;
using MundoAwoo.Infrastructure.Security;
using MundoAwoo.Application.Security;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Awoorld API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el JWT recibido al registrarte o iniciar sesión."
    });
    options.OperationFilter<JwtAuthorizeOperationFilter>();
});

// La conexión real se configura en User Secrets o variables de entorno, fuera del repositorio.
string? conexion = builder.Configuration.GetConnectionString("Awoorld");
if (string.IsNullOrWhiteSpace(conexion))
    throw new InvalidOperationException("Configura ConnectionStrings:Awoorld. Consulta docs/03-mysql.md.");

string versionMySql = builder.Configuration["Database:ServerVersion"] ?? "8.4.0";
builder.Services.AddDbContext<AwoorldDbContext>(options =>
    options.UseMySql(conexion, new MySqlServerVersion(Version.Parse(versionMySql))));
// La versión explícita permite generar migraciones sin conectarse al servidor.
// Aplicaremos las migraciones con dotnet ef database update, no en cada arranque de la API.

// Scoped crea una instancia por solicitud HTTP. AddDbContext también usa ese alcance:
// los tres repositorios y la unidad de trabajo comparten el mismo contexto y sus cambios.
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAvatarRepository, AvatarRepository>();
builder.Services.AddScoped<IItemDeAvatarRepository, ItemDeAvatarRepository>();
builder.Services.AddScoped<ICatalogoAdministracionRepository, CatalogoAdministracionRepository>();
builder.Services.AddScoped<ISesionRefreshTokenRepository, SesionRefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// El servicio de perfil recibirá los repositorios y la unidad de trabajo del mismo alcance.
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAvatarService, AvatarService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IAvatarAplicacionService, AvatarAplicacionService>();
builder.Services.AddScoped<ICatalogoConsultaService, CatalogoConsultaService>();
builder.Services.AddScoped<IInventarioConsultaService, InventarioConsultaService>();
builder.Services.AddScoped<IInventarioAdministracionService, InventarioAdministracionService>();
builder.Services.AddScoped<IAccesoAdministradorService, AccesoAdministradorService>();
builder.Services.AddScoped<ICatalogoAdministracionService, CatalogoAdministracionService>();
builder.Services.AddScoped<IAuthorizationHandler, AdministradorHandler>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();

var app = builder.Build();

// Debe envolver los componentes posteriores para capturar sus excepciones.
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Primero validamos el token y después comprobamos los permisos del endpoint.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite iniciar la API dentro de las pruebas HTTP con WebApplicationFactory.
public partial class Program { }
