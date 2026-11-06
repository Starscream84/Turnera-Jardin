using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    public AppointmentsController(AppDbContext db, IEmailQueue emails, ILogger<AppointmentsController> logger)
    {
        _db = db;
        _emails = emails;
        _logger = logger;
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
            return NotFound(new { message = "El turno no existe." });
        }

        var phone = PhoneNumber.Normalize(dto.GuardianPhone)!; // [ArgentinePhone] already validated it
        var email = dto.GuardianEmail.Trim();

        if (appointment.Status == AppointmentStatus.Booked)
        {
            return IsSameFamily(appointment, phone, email)
                ? Ok(ToBookedDto(appointment))
                : Conflict(new { message = "Este turno ya no está disponible. Elegí otro horario." });
        }

        if (appointment.Status != AppointmentStatus.Available || !appointment.Teacher!.IsActive)
        {
            return Conflict(new { message = "Este turno ya no está disponible. Elegí otro horario." });
        }

        if (appointment.Date < ArgentinaDate.FirstBookableDay())
        {
            return BadRequest(new { message = "Los turnos se reservan con al menos un día de anticipación. Elegí un turno a partir de mañana." });
        }

        if (appointment.Date > ArgentinaDate.LastBookableDay())
        {
            return BadRequest(new { message = $"Por ahora se puede reservar hasta {ArgentinaDate.BookingDaysAhead} días antes. Elegí una fecha más cercana." });
        }

        var mode = dto.Mode!.Value;
        // First cycle and specialists: interviews are always in person and the family does not choose.
        if (appointment.Teacher!.Cycle is TeacherCycle.FirstCycle or TeacherCycle.Specialist)
        {
            if (mode == InterviewMode.Online)
            {
                return BadRequest(new { message = "Esta docente solo atiende entrevistas presenciales." });
            }

            mode = InterviewMode.InPerson;
        }

        var bookedAt = DateTime.UtcNow;
        var guardianFirstName = dto.GuardianFirstName.Trim();
        var guardianLastName = dto.GuardianLastName.Trim();
        var childFirstName = dto.ChildFirstName.Trim();
        var childLastName = dto.ChildLastName.Trim();
        var notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        var firstDay = ArgentinaDate.FirstBookableDay();
        var lastDay = ArgentinaDate.LastBookableDay();

        var rows = await _db.Appointments
            .Where(a => a.Id == id
                        && a.Status == AppointmentStatus.Available
                        && a.Date >= firstDay
                        && a.Date <= lastDay
                        && a.Teacher!.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, AppointmentStatus.Booked)
                .SetProperty(a => a.GuardianFirstName, guardianFirstName)
                .SetProperty(a => a.GuardianLastName, guardianLastName)
                .SetProperty(a => a.GuardianPhone, phone)
                .SetProperty(a => a.GuardianEmail, email)
                .SetProperty(a => a.ChildFirstName, childFirstName)
                .SetProperty(a => a.ChildLastName, childLastName)
                .SetProperty(a => a.Notes, notes)
                .SetProperty(a => a.Mode, (InterviewMode?)mode)
                .SetProperty(a => a.BookedAt, (DateTime?)bookedAt)
                .SetProperty(a => a.ConfirmationSent, false)
                .SetProperty(a => a.ReminderSent, false)
                .SetProperty(a => a.ConfirmationRequestedAt, (DateTime?)null), ct);

        if (rows == 0)
        {
            // Someone else won the slot between our read and the UPDATE (or it is our own earlier request).
            var current = await _db.Appointments.AsNoTracking().Include(a => a.Teacher)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            return current is not null && current.Status == AppointmentStatus.Booked && IsSameFamily(current, phone, email)
                ? Ok(ToBookedDto(current))
                : Conflict(new { message = "Este turno ya no está disponible. Elegí otro horario." });
        }

        // The email goes out in the background (EmailDispatcherService), with retries if Brevo fails.
        _emails.EnqueueConfirmation(id);
        _logger.LogInformation("Appointment {AppointmentId} booked", id);

        appointment.Status = AppointmentStatus.Booked;
        appointment.ChildFirstName = childFirstName;
        appointment.ChildLastName = childLastName;
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
            return BadRequest(new { message = "Ingresá el WhatsApp con el que reservaste." });
        }

        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

        if (appointment is null
            || appointment.Status != AppointmentStatus.Booked
            || !PhoneNumber.SameNumber(enteredPhone, appointment.GuardianPhone))
        {
            return NotFound(new { message = NotFoundOrNoMatchMessage });
        }

        if (appointment.Date < ArgentinaDate.FirstBookableDay())
        {
            return BadRequest(new { message = "Los turnos de hoy o de días anteriores no se pueden cancelar desde la web. Comunicate con el jardín." });
        }

        // Status and BookedAt are concurrency tokens: if the appointment changed since we read it,
        // SaveChanges throws and ApiExceptionHandler answers 409.
        appointment.ClearBooking();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Appointment {AppointmentId} cancelled by the family", id);
        return NoContent();
    }

    private static bool IsSameFamily(Appointment appointment, string normalizedPhone, string email) =>
        PhoneNumber.SameNumber(normalizedPhone, appointment.GuardianPhone)
        && string.Equals(appointment.GuardianEmail?.Trim(), email, StringComparison.OrdinalIgnoreCase);

    private static BookedAppointmentDto ToBookedDto(Appointment appointment) => new(
        appointment.Id,
        appointment.Teacher!.FullName,
        appointment.Date,
        appointment.StartTime,
        appointment.EndTime,
        appointment.ChildFirstName ?? "",
        appointment.ChildLastName ?? "",
        appointment.Mode ?? InterviewMode.InPerson,
        appointment.Mode == InterviewMode.InPerson ? appointment.Location : null);
}
