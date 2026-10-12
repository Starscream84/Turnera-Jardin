# Script para aplicar todos los cambios necesarios al proyecto TurneraJardin.Api
# Ejecutar desde: C:\Users\Usuario\Desktop\ProyectoTurnera\Turnera-Jardin\backend\TurneraJardin.Api

Write-Host "=== Aplicando todos los cambios necesarios ===" -ForegroundColor Green

# 1. Crear carpeta Options
Write-Host "`n[1/6] Creando carpeta Options..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path "Options" -ErrorAction SilentlyContinue | Out-Null

# 2. Crear JwtOptions.cs
Write-Host "[2/6] Creando JwtOptions.cs..." -ForegroundColor Cyan
$jwtOptions = @"
namespace TurneraJardin.Api.Options;

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
}
"@
$jwtOptions | Out-File "Options\JwtOptions.cs" -Encoding UTF8 -Force

# 3. Crear JwtService.cs (corregido)
Write-Host "[3/6] Creando JwtService.cs (corregido)..." -ForegroundColor Cyan
$jwtService = @"
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Options;

namespace TurneraJardin.Api.Services;

public class TokenResult
{
    public string Token { get; set; }
    public TokenResult(string token) { Token = token; }
}

public interface IJwtService
{
    TokenResult GenerarToken(Usuario usuario);
}

public class JwtService : IJwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenResult GenerarToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new("VersionSesion", usuario.VersionSesion.ToString())
        };

        if (usuario.DocenteId.HasValue)
        {
            claims.Add(new Claim("DocenteId", usuario.DocenteId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return new TokenResult(tokenHandler.WriteToken(token));
    }
}
"@
$jwtService | Out-File "Services\JwtService.cs" -Encoding UTF8 -Force

# 4. Crear AuditoriaService.cs
Write-Host "[4/6] Creando AuditoriaService.cs..." -ForegroundColor Cyan
$auditoriaService = @"
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface IAuditoriaService
{
    Task RegistrarAsync(string accion, string entidad, string? entidadId, string? detalle);
}

public class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditoriaService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RegistrarAsync(string accion, string entidad, string? entidadId, string? detalle)
    {
        try
        {
            var usuarioId = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var usuarioEmail = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Sistema";

            var auditoria = new AuditoriaLog
            {
                FechaUtc = DateTime.UtcNow,
                UsuarioId = int.TryParse(usuarioId, out var id) ? id : (int?)null,
                UsuarioEmail = usuarioEmail,
                Accion = accion.Length > 60 ? accion.Substring(0, 60) : accion,
                Entidad = entidad.Length > 60 ? entidad.Substring(0, 60) : entidad,
                EntidadId = entidadId?.Length > 40 ? entidadId.Substring(0, 40) : entidadId,
                Detalle = detalle?.Length > 500 ? detalle.Substring(0, 500) : detalle
            };

            _db.AuditoriaLogs.Add(auditoria);
            await _db.SaveChangesAsync();
        }
        catch { }
    }
}
"@
$auditoriaService | Out-File "Services\AuditoriaService.cs" -Encoding UTF8 -Force

# 5. Crear DocenteDtos.cs
Write-Host "[5/6] Creando DocenteDtos.cs..." -ForegroundColor Cyan
$docenteDtos = @"
namespace TurneraJardin.Api.Dtos;

public class DocenteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class DocenteAdminDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int? UsuarioId { get; set; }
}

public class DocenteCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
}

public class DocenteUpdateDto
{
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool? Activo { get; set; }
}

public class CrearDisponibilidadDto
{
    public int DocenteId { get; set; }
    public int DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}
"@
$docenteDtos | Out-File "Dtos\DocenteDtos.cs" -Encoding UTF8 -Force

# 6. Actualizar Program.cs
Write-Host "[6/6] Actualizando Program.cs..." -ForegroundColor Cyan
$programCs = @"
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TurneraJardin.Api.Auth;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Options;
using TurneraJardin.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<ITurnosService, TurnosService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddHostedService<RecordatorioBackgroundService>();

builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.SeccionConfig));
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppCloudApiService>();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "TuSuperClaveSecretaQueDebeSerLarga123!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TurneraJardinApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TurneraJardinClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Politicas.SoloSuperAdmin, policy =>
        policy.RequireRole(nameof(RolUsuario.SuperAdmin)));

    options.AddPolicy(Politicas.Direccion, policy =>
        policy.RequireRole(nameof(RolUsuario.Admin), nameof(RolUsuario.SuperAdmin)));

    options.AddPolicy(Politicas.Personal, policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Turnera Jardín API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresá el token con el formato: Bearer {tu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Turnera Jardín API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
"@
$programCs | Out-File "Program.cs" -Encoding UTF8 -Force

Write-Host "`n✅ ¡Todos los cambios aplicados exitosamente!" -ForegroundColor Green
Write-Host "`nAhora ejecuta: dotnet build" -ForegroundColor Yellow
