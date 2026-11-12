using Microsoft.AspNetCore.Mvc;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Stable, machine-readable error codes. Every error response carries one in "code", next to the
/// human text in "message", so the frontend (or anyone using the API) can react to a specific error
/// without depending on the wording. The full list is in docs/migraciones-y-errores.md.
/// Never rename an existing code: add a new one instead.
/// </summary>
public static class ErrorCodes
{
    // Generic
    public const string ValidationError = "VALIDATION_ERROR";
    public const string RateLimited = "RATE_LIMITED";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string DuplicateKey = "DUPLICATE_KEY";
    public const string InternalError = "INTERNAL_ERROR";
    public const string Forbidden = "FORBIDDEN";

    // Session and accounts
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string PasswordChangeRequired = "PASSWORD_CHANGE_REQUIRED";
    public const string WrongCurrentPassword = "WRONG_CURRENT_PASSWORD";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string UserEmailInUse = "USER_EMAIL_IN_USE";
    public const string TeacherHasUser = "TEACHER_HAS_USER";
    public const string LastAdmin = "LAST_ADMIN";
    public const string InvalidImage = "INVALID_IMAGE";

    // Teachers and slot generation
    public const string TeacherNotFound = "TEACHER_NOT_FOUND";
    public const string TeacherInactive = "TEACHER_INACTIVE";
    public const string TeacherEmailInUse = "TEACHER_EMAIL_IN_USE";
    public const string InvalidDateRange = "INVALID_DATE_RANGE";
    public const string DateRangeTooLong = "DATE_RANGE_TOO_LONG";
    public const string InvalidTimeRange = "INVALID_TIME_RANGE";
    public const string InvalidLocation = "INVALID_LOCATION";
    public const string TooManyAppointments = "TOO_MANY_APPOINTMENTS";

    // Appointments
    public const string AppointmentNotFound = "APPOINTMENT_NOT_FOUND";
    public const string AppointmentTaken = "APPOINTMENT_TAKEN";
    public const string AppointmentNotBooked = "APPOINTMENT_NOT_BOOKED";
    public const string AppointmentAlreadyBooked = "APPOINTMENT_ALREADY_BOOKED";
    public const string BookingTooSoon = "BOOKING_TOO_SOON";
    public const string BookingTooFar = "BOOKING_TOO_FAR";
    public const string ModeNotAllowed = "MODE_NOT_ALLOWED";
    public const string PhoneRequired = "PHONE_REQUIRED";
    public const string BookingNotFound = "BOOKING_NOT_FOUND";
    public const string CancelTooLate = "CANCEL_TOO_LATE";
}

/// <summary>
/// Builds the single error format of the API: RFC 9457 ProblemDetails (application/problem+json) with
///   status, title (= message), code, message and requestId.
/// "message" duplicates "title" on purpose: the frontend has always read "message".
/// </summary>
public static class ApiProblem
{
    public const string ContentType = "application/problem+json";

    public static ProblemDetails Build(HttpContext context, int status, string code, string message)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = message,
            Type = $"urn:turnera-jardin:error:{code}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["message"] = message;
        problem.Extensions["requestId"] = context.TraceIdentifier;
        return problem;
    }

    public static ObjectResult Result(HttpContext context, int status, string code, string message)
    {
        var result = new ObjectResult(Build(context, status, code, message)) { StatusCode = status };
        result.ContentTypes.Add(ContentType);
        return result;
    }

    /// <summary>Writes the problem directly to the response (for middlewares, where there is no controller).</summary>
    public static Task WriteAsync(HttpContext context, int status, string code, string message, CancellationToken ct = default)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(Build(context, status, code, message), (System.Text.Json.JsonSerializerOptions?)null, ContentType, ct);
    }
}

public static class ControllerErrorExtensions
{
    /// <summary>Error response in the API's single format. E.g. return this.ApiError(404, ErrorCodes.TeacherNotFound, "Docente no encontrado.").</summary>
    public static ObjectResult ApiError(this ControllerBase controller, int status, string code, string message) =>
        ApiProblem.Result(controller.HttpContext, status, code, message);
}
