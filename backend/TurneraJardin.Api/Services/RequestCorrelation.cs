using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Gives every request an id and puts it everywhere it is useful:
///  - In the "X-Request-Id" response header (the frontend shows it next to unexpected errors as "código").
///  - In every log line written during the request (logging scope "RequestId"), so all the lines of one
///    request can be found together in Railway's log search.
///  - In the "requestId" field of every error response.
/// If the caller sends a sane X-Request-Id, it is reused. At the end, one summary line per request
/// (method, path, status, duration) is logged, except for /health.
/// </summary>
public sealed partial class RequestCorrelationMiddleware
{
    public const string HeaderName = "X-Request-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    public RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{8,64}$")]
    private static partial Regex SafeId();

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var requestId = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N")[..16];
        context.TraceIdentifier = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId });
        var watch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            watch.Stop();
            if (!context.Request.Path.StartsWithSegments("/health"))
            {
                // Path only: query strings can carry personal data (e.g. old "?phone=" links).
                _logger.LogInformation("HTTP {Method} {Path} -> {Status} in {ElapsedMs} ms",
                    context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, watch.ElapsedMilliseconds);
            }
        }
    }
}

/// <summary>
/// H30: while a user has a temporary password (User.MustChangePassword), every authenticated endpoint
/// except changing the password answers 403 PASSWORD_CHANGE_REQUIRED. The flag is read from the database
/// in OnTokenValidated (Program.cs), so it takes effect without a new login.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware
{
    public const string ItemKey = "MustChangePassword";

    private static readonly string[] AllowedPaths = { "/api/auth/change-password" };

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var mustChange = context.Items.TryGetValue(ItemKey, out var value) && value is true;
        if (mustChange && !AllowedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await ApiProblem.WriteAsync(context, StatusCodes.Status403Forbidden, ErrorCodes.PasswordChangeRequired,
                "Tenés que cambiar la contraseña temporal antes de seguir. Entrá a \"Mi cuenta\".");
            return;
        }

        await _next(context);
    }
}
