using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Last line of defense for exceptions nobody caught. Instead of an empty 500, it answers in the same
/// format as the rest of the API ({ message } inside a ProblemDetails body) and maps database
/// conflicts to 409:
///  - DbUpdateConcurrencyException: someone else changed the same appointment in the meantime.
///  - MySQL 1062 (duplicate key): e.g. two coordinators generating the same slots at the same time,
///    or two users created for the same teacher.
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problemDetails)
    {
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "Otra persona modificó este turno recién. Actualizá la pantalla y volvé a intentar."),
            DbUpdateException { InnerException: MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry } } =>
                (StatusCodes.Status409Conflict, "Ya existe un registro con esos datos (por ejemplo, un turno en ese horario o un usuario con ese email)."),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                (499, "El pedido se canceló."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intentá de nuevo en un momento.")
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled error on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("{ExceptionType} on {Method} {Path} answered as {Status}",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, status);
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = message,
        };
        // The frontend reads error.message (same as the { message } answers of the controllers).
        problem.Extensions["message"] = message;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
