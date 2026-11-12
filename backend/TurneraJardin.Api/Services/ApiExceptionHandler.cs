using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Last line of defense for exceptions nobody caught. Instead of an empty 500, it answers in the API's
/// single error format (ProblemDetails with code, message and requestId) and maps database conflicts to 409:
///  - DbUpdateConcurrencyException: someone else changed the same appointment in the meantime.
///  - MySQL 1062 (duplicate key): e.g. two coordinators generating the same slots at the same time,
///    or two users created for the same teacher.
/// The requestId is the same one written in every log line of that request, so a family can quote it
/// ("código de error") and the log can be found in Railway.
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, message) = exception switch
        {
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, ErrorCodes.ConcurrencyConflict,
                 "Otra persona modificó este turno recién. Actualizá la pantalla y volvé a intentar."),
            DbUpdateException { InnerException: MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry } } =>
                (StatusCodes.Status409Conflict, ErrorCodes.DuplicateKey,
                 "Ya existe un registro con esos datos (por ejemplo, un turno en ese horario o un usuario con ese email)."),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                (499, ErrorCodes.InternalError, "El pedido se canceló."),
            _ =>
                (StatusCodes.Status500InternalServerError, ErrorCodes.InternalError,
                 "Ocurrió un error inesperado. Intentá de nuevo en un momento.")
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled error on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("{ExceptionType} on {Method} {Path} answered as {Status} {Code}",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, status, code);
        }

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        await ApiProblem.WriteAsync(httpContext, status, code, message, cancellationToken);
        return true;
    }
}
