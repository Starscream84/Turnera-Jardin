using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>Public endpoints for a parent to book or cancel their appointment. No authentication.</summary>
[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private const string NotFoundOrNoMatchMessage = "No encontramos un turno reservado con ese número y ese teléfono. Revisá los datos.";

    private readonly AppDbContext _db;
    private readonly IEmailQueue _emails;
    private readonly ILogger<AppointmentsController> _logger;
    private readonly IOutputCacheStore _cache;

    public AppointmentsController(AppDbContext db, IEmailQueue emails, ILogger<AppointmentsController> logger, IOutputCacheStore cache)
    {
        _db = db;
        _emails = emails;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// One free slot, by id: what the booking form (step 3) needs, instead of downloading every free
    /// slot of the teacher. 404 if it does not exist, is already booked, or is not bookable (today, past,
    /// more than 6 months ahead, inactive teacher).
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AvailableAppointmentDto>> GetOne(int id)
    {
        var firstDay = ArgentinaDate.FirstBookableDay();
        var lastDay = ArgentinaDate.LastBookableDay();

        var dto = await _db.Appointments.AsNoTracking()
            .Where(a => a.Id == id
                        && a.Teacher!.IsActive
                        && a.Status == AppointmentStatus.Available
                        && a.Date >= firstDay
                        && a.Date <= lastDay)
            .Select(a => new AvailableAppointmentDto(
                a.Id,
                a.TeacherId,
                a.Teacher!.FirstName + " " + a.Teacher!.LastName,
                a.Teacher!.Classroom ?? "Sin asignar",
                a.Date,
                a.StartTime,
                a.EndTime,
                a.Status.ToString(),
                a.Location))
            .FirstOrDefaultAsync();

        return dto is null
            ? this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.AppointmentNotFound, "Este turno ya no está disponible. Volvé y elegí otro horario.")
            : Ok(dto);
    }

    /// <summary>
    /// Free slots of one teacher (teacherId is required). Only bookable dates (from tomorrow up to
    /// 6 months ahead) and only active teachers. With "date", only that day.
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<AvailableAppointmentDto>>> GetAvailable(
        [FromQuery, BindRequired] int teacherId,
        [FromQuery] DateOnly? date)
    {
        var firstDay = ArgentinaDate.FirstBookableDay();
        var lastDay = ArgentinaDate.LastBookableDay();

        var query = _db.Appointments.AsNoTracking()
            .Where(a => a.TeacherId == teacherId
                        && a.Teacher!.IsActive
                        && a.Status == AppointmentStatus.Available
                        && a.Date >= firstDay
                        && a.Date <= lastDay);

        if (date is not null)
        {
            query = query.Where(a => a.Date == date);
        }

        var dtos = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(a => new AvailableAppointmentDto(
                a.Id,
                a.TeacherId,
                a.Teacher!.FirstName + " " + a.Teacher!.LastName,
                a.Teacher!.Classroom ?? "Sin asignar",
                a.Date,
                a.StartTime,
                a.EndTime,
                a.Status.ToString(),
                a.Location))
            .ToListAsync();

        return Ok(dtos);
    }

    /// <summary>
    /// Books a slot. The booking is a single conditional UPDATE ("... WHERE Id = id AND Status = Available"):
    /// if two families press "Confirmar" at the same time, only one gets 200 and the other gets 409.
    /// If the same family retries after losing the connection (same phone and email), it gets 200 with
    /// the same booking instead of a misleading 409.
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    [HttpPost("{id:int}/book")]
    public async Task<ActionResult<BookedAppointmentDto>> Book(int id, BookAppointmentDto dto, CancellationToken ct)
    {
        var appointment = await _db.Appointments.AsNoTracking()
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (appointment is null)
        {
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.AppointmentNotFound, "El turno no existe.");
        }

        var phone = PhoneNumber.Normalize(dto.GuardianPhone)!; // [ArgentinePhone] already validated it
        var email = dto.GuardianEmail.Trim();

        if (appointment.Status == AppointmentStatus.Booked)
        {
            return IsSameFamily(appointment, phone, email)
                ? Ok(ToBookedDto(appointment))
                : this.ApiError(StatusCodes.Status409Conflict, ErrorCodes.AppointmentTaken, "Este turno ya no está disponible. Elegí otro horario.");
        }

        if (appointment.Status != AppointmentStatus.Available || !appointment.Teacher!.IsActive)
        {
            return this.ApiError(StatusCodes.Status409Conflict, ErrorCodes.AppointmentTaken, "Este turno ya no está disponible. Elegí otro horario.");
        }

        if (appointment.Date < ArgentinaDate.FirstBookableDay())
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.BookingTooSoon, "Los turnos se reservan con al menos un día de anticipación. Elegí un turno a partir de mañana.");
        }

        if (appointment.Date > ArgentinaDate.LastBookableDay())
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.BookingTooFar, $"Por ahora se puede reservar hasta {ArgentinaDate.BookingDaysAhead} días antes. Elegí una fecha más cercana.");
        }

        // Same rule for every teacher: the child's first interview is in person, the rest online.
        // The family tells us in step 3 (there is no family record to check it against).
        var mode = dto.Mode!.Value;

        var bookedAt = DateTime.UtcNow;
        var guardianName = NormalizeName(dto.GuardianName);
        var childName = NormalizeName(dto.ChildName);
        var notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        var firstDay = ArgentinaDate.FirstBookableDay();
        var lastDay = ArgentinaDate.LastBookableDay();

        // The conditional UPDATE and the history row are saved together (same transaction). The execution
        // strategy (EnableRetryOnFailure) re-runs the whole block if MySQL drops the connection midway.
        var strategy = _db.Database.CreateExecutionStrategy();
        var rows = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            _db.ChangeTracker.Clear();

            var updated = await _db.Appointments
                .Where(a => a.Id == id
                            && a.Status == AppointmentStatus.Available
                            && a.Date >= firstDay
                            && a.Date <= lastDay
                            && a.Teacher!.IsActive)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, AppointmentStatus.Booked)
                    .SetProperty(a => a.GuardianName, guardianName)
                    .SetProperty(a => a.GuardianPhone, phone)
                    .SetProperty(a => a.GuardianEmail, email)
                    .SetProperty(a => a.ChildName, childName)
                    .SetProperty(a => a.Notes, notes)
                    .SetProperty(a => a.Mode, (InterviewMode?)mode)
                    .SetProperty(a => a.BookedAt, (DateTime?)bookedAt)
                    .SetProperty(a => a.ConfirmationSent, false)
                    .SetProperty(a => a.ReminderSent, false)
                    .SetProperty(a => a.ConfirmationRequestedAt, (DateTime?)null), ct);

            if (updated == 1)
            {
                var booked = appointment.SnapshotForEmail();
                booked.GuardianName = guardianName;
                booked.GuardianPhone = phone;
                booked.ChildName = childName;
                booked.Mode = mode;
                _db.AppointmentEvents.Add(AppointmentEvent.From(booked, AppointmentEventType.Booked));
                await _db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
            return updated;
        });

        if (rows == 0)
        {
            // Someone else won the slot between our read and the UPDATE (or it is our own earlier request).
            var current = await _db.Appointments.AsNoTracking().Include(a => a.Teacher)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            return current is not null && current.Status == AppointmentStatus.Booked && IsSameFamily(current, phone, email)
                ? Ok(ToBookedDto(current))
                : this.ApiError(StatusCodes.Status409Conflict, ErrorCodes.AppointmentTaken, "Este turno ya no está disponible. Elegí otro horario.");
        }

        // The email goes out in the background (EmailDispatcherService), with retries if Brevo fails.
        _emails.EnqueueConfirmation(id);
        await _cache.EvictByTagAsync(CachePolicies.AvailabilityTag, ct);
        _logger.LogInformation("Appointment {AppointmentId} booked", id);

        appointment.Status = AppointmentStatus.Booked;
        appointment.ChildName = childName;
        appointment.Mode = mode;
        return Ok(ToBookedDto(appointment));
    }

    /// <summary>
    /// Cancels a booked appointment and frees it as available so another family can book it.
    /// As basic security, the same phone number used when booking is required (in the body).
    /// "Does not exist" and "the phone does not match" get the same answer, so the endpoint can't be
    /// used to find out which appointment numbers are booked.
    /// Appointments for today or past dates can't be cancelled from the web.
    /// </summary>
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CancelAppointmentDto? dto,
        [FromQuery] string? phone,
        CancellationToken ct)
    {
        // "?phone=" is still accepted for a while so an older frontend keeps working during the deploy.
        var enteredPhone = dto?.Phone ?? phone;
        if (string.IsNullOrWhiteSpace(enteredPhone))
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.PhoneRequired, "Ingresá el WhatsApp con el que reservaste.");
        }

        var appointment = await _db.Appointments.Include(a => a.Teacher).FirstOrDefaultAsync(a => a.Id == id, ct);

        if (appointment is null
            || appointment.Status != AppointmentStatus.Booked
            || !PhoneNumber.SameNumber(enteredPhone, appointment.GuardianPhone))
        {
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.BookingNotFound, NotFoundOrNoMatchMessage);
        }

        if (appointment.Date < ArgentinaDate.FirstBookableDay())
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.CancelTooLate, "Los turnos de hoy o de días anteriores no se pueden cancelar desde la web. Comunicate con el jardín.");
        }

        // Status and BookedAt are concurrency tokens: if the appointment changed since we read it,
        // SaveChanges throws and ApiExceptionHandler answers 409.
        // The history row and the cleared slot are saved in the same SaveChanges (one transaction).
        _db.AppointmentEvents.Add(AppointmentEvent.From(appointment, AppointmentEventType.CancelledByFamily));
        appointment.ClearBooking();
        await _db.SaveChangesAsync(ct);
        await _cache.EvictByTagAsync(CachePolicies.AvailabilityTag, ct);

        _logger.LogInformation("Appointment {AppointmentId} cancelled by the family", id);
        return NoContent();
    }

    private static bool IsSameFamily(Appointment appointment, string normalizedPhone, string email) =>
        PhoneNumber.SameNumber(normalizedPhone, appointment.GuardianPhone)
        && string.Equals(appointment.GuardianEmail?.Trim(), email, StringComparison.OrdinalIgnoreCase);

    /// <summary>Full name as typed in the form, without extra spaces ("  Ana   María  Pérez " → "Ana María Pérez").</summary>
    private static string NormalizeName(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static BookedAppointmentDto ToBookedDto(Appointment appointment) => new(
        appointment.Id,
        appointment.Teacher!.FullName,
        appointment.Date,
        appointment.StartTime,
        appointment.EndTime,
        appointment.ChildName ?? "",
        appointment.Mode ?? InterviewMode.InPerson,
        appointment.Mode == InterviewMode.InPerson ? appointment.Location : null);
}
