using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Logs: one JSON object per line outside Development (Railway indexes the fields, e.g. RequestId),
// including the scope with the request id added by RequestCorrelationMiddleware. Plain text locally.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
        options.UseUtcTimestamp = true;
    });
}

// --- Database (MySQL) ---
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default is missing");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 11)),
        // Retries transient errors (MySQL restarting, a dropped connection) instead of failing the request
        // or leaving the container in a restart loop at startup. There are no manual transactions left
        // (booking is a single conditional UPDATE), so it is compatible with every endpoint.
        mysql => mysql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null))
    // Short migration lock name: Railway's 48-character database name made Pomelo's exceed MySQL's 64-character limit.
    .ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IHistoryRepository, ShortLockMySqlHistoryRepository>());

// --- WhatsApp Cloud API settings (disabled: the teacher sends the message with the "Confirmación" button) ---
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.ConfigSection));
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppCloudApiService>();

// --- Emails (Brevo HTTP API), sent in the background so booking never waits for Brevo ---
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.ConfigSection));
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
builder.Services.AddHostedService<EmailDispatcherService>();

// --- App services ---
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginAttemptTracker>();
// ReminderBackgroundService (automatic WhatsApp reminder) stays unregistered on purpose.

// --- Errors: every unhandled exception is answered as ProblemDetails with { message } (409 for DB conflicts) ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// --- Health check: GET /health answers 200 only if MySQL is reachable ---
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

// --- Short cache for the public reads (teachers list, free slots of a teacher). Every change to the
// slots (book, cancel, generate, delete) evicts the "availability" tag, so a family never sees a stale
// free slot for more than an instant; the teachers list is evicted when a teacher is created or edited.
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(CachePolicies.Teachers, p => p.Expire(TimeSpan.FromMinutes(5)).Tag(CachePolicies.TeachersTag));
    options.AddPolicy(CachePolicies.Availability, p => p.Expire(TimeSpan.FromSeconds(30)).Tag(CachePolicies.AvailabilityTag));
});

// --- JWT authentication ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // A valid signature is not enough: the user must still exist, be active, have the same role and
    // teacher, and the same password as when the token was issued. This is what lets "deactivate user"
    // and "reset password" cut access immediately instead of after up to 8 hours.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal!;
            if (!int.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            {
                context.Fail("Invalid token");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.IsActive, u.PasswordHash, u.Role, u.TeacherId, u.MustChangePassword })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            var stamp = principal.FindFirst(SecurityStamp.ClaimName)?.Value;
            var roleInToken = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var teacherInToken = principal.FindFirst(JwtService.TeacherIdClaim)?.Value;

            if (user is null
                || !user.IsActive
                || stamp != SecurityStamp.For(user.Id, user.PasswordHash)
                || roleInToken != user.Role.ToString()
                || teacherInToken != user.TeacherId?.ToString())
            {
                context.Fail("Session no longer valid");
                return;
            }

            // H30: read by PasswordChangeRequiredMiddleware.
            context.HttpContext.Items[PasswordChangeRequiredMiddleware.ItemKey] = user.MustChangePassword;
        }
    };
});

// Explicit policies instead of deducing the scope "by elimination" (see AdminAppointmentsController.TryGetScope).
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Management, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Coordinator)))
    .AddPolicy(Policies.Panel, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Coordinator), nameof(UserRole.Teacher)));

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
            .AllowAnyMethod()
            .WithExposedHeaders("Retry-After", RequestCorrelationMiddleware.HeaderName, "X-Total-Count", "X-Booked-Count");
    });
});

// Enums (e.g. appointment Status) travel as text ("Available", "Booked"...) instead of numbers,
// so the frontend does not need to know the internal numeric values.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        // Validation errors (400): same format as the rest of the API (code VALIDATION_ERROR, message = first
        // error) plus "errors" with every field, as in ValidationProblemDetails.
        options.InvalidModelStateResponseFactory = context =>
        {
            var first = context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Hay datos inválidos en el formulario.";
            var problem = ApiProblem.Build(context.HttpContext, StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, first);
            problem.Extensions["errors"] = context.ModelState
                .Where(kv => kv.Value is { Errors.Count: > 0 })
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            var result = new BadRequestObjectResult(problem);
            result.ContentTypes.Add(ApiProblem.ContentType);
            return result;
        };
    });
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

// -- Real client IP behind the Railway proxy.
// Only the last X-Forwarded-For entry is used (ForwardLimit = 1): that is the one Railway's proxy adds,
// so a client can't fake its IP by sending its own header. The container is only reachable through
// that proxy. If the proxy range is known it can be restricted with ForwardedHeaders__KnownNetworks__0=10.0.0.0/8.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    var knownNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var cidr in knownNetworks)
    {
        var parts = cidr.Split('/');
        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ip) && int.TryParse(parts[1], out var prefix))
        {
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ip, prefix));
        }
    }
});

// -- Request limits per IP.
// Limits are generous enough for a shared IP (school wifi, mobile carrier CGNAT): the real protection
// against booking abuse is that each booking is one atomic UPDATE and each slot can only be taken once.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }
        await ApiProblem.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, ErrorCodes.RateLimited,
            "Hiciste muchos intentos seguidos. Esperá unos minutos y probá de nuevo.", ct);
    };

    static string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Book and cancel: 20 requests every 10 minutes per IP.
    options.AddPolicy(RateLimitPolicies.PublicWrite, context =>
        RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));

    // Public reads (teachers and free slots): 120 per minute per IP.
    options.AddPolicy(RateLimitPolicies.PublicRead, context =>
        RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Login: 10 attempts every 5 minutes per IP (plus the per-account lock of LoginAttemptTracker).
    options.AddPolicy(RateLimitPolicies.Login, context =>
        RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
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
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    DbSeeder.Seed(db, app.Configuration, app.Environment, seedLogger);
}

app.UseForwardedHeaders();
app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic security headers for the API responses (the frontend ones are in vercel.json).
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseStaticFiles(); // serves wwwroot/fotos-perfil (users' profile photos, on the Railway volume)
app.UseCors(CorsPolicyFrontend);
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
