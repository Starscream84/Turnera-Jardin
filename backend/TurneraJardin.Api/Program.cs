using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Database (MySQL) ---
var connectionString = builder.Configuration.GetConnectionString("Default") 
    ?? throw new InvalidOperationException("ConnectionStrings:Default is missing");

var csb = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
    Console.WriteLine($"[DB] Host: {csb.Server} | Port: {csb.Port} | Database: {csb.Database}");
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 11))));
    
// --- WhatsApp Cloud API settings ---
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.ConfigSection));
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppCloudApiService>();

// --- Booking confirmation email (Brevo HTTP API) ---
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.ConfigSection));
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();

// --- App services ---
builder.Services.AddScoped<IJwtService, JwtService>();
// The day-before reminder is not automatic: the teacher sends it with the "Confirmación" button
// in the appointment list. ReminderBackgroundService (automatic WhatsApp sending) stays unregistered.

// --- JWT authentication ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ;
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key is not configured (environment variable Jwt__Key) or is shorter than 32 characters.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Without this, ASP.NET Core remaps the "sub" claim to a different internal name,
    // and the code that looks for JwtRegisteredClaimNames.Sub (in AuthController) can no longer find it.
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorization();

// --- CORS: enabled so the Angular frontend (on another origin) can call the API ---
const string CorsPolicyFrontend = "FrontendPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyFrontend, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Enums (e.g. appointment Status) travel as text ("Available", "Booked"...) instead of numbers,
// so the frontend does not need to know the internal numeric values.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Turnera Jardín API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token with the 'Bearer ' prefix. E.g.: Bearer eyJhbGciOi...",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

// -- Real client IP behind the Railway proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =  ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// -- Request limits per IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Book and cancel: 5 requests every 10 minutes per IP
    options.AddPolicy("public", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));

    // Login: 10 attempts every 5 minutes per IP
    options.AddPolicy("login", context => 
    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
    _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 10,
        Window = TimeSpan.FromMinutes(5),
        QueueLimit = 0
    }));
});


var app = builder.Build();

// --- Creates the database and loads the seed data the first time it starts ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.Seed(db);
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // serves wwwroot/fotos-perfil (users' profile photos)
app.UseCors(CorsPolicyFrontend);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
