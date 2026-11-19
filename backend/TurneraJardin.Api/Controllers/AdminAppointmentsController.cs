using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>
/// Appointment list and management for the admin panel. Admin and Coordinator see everything;
/// a user with the Teacher role can only see/manage the appointments of their own schedule.
/// Any other case (e.g. a Teacher user whose teacher was deleted) is denied: the scope is never
/// deduced "by elimination".
/// </summary>
[ApiController]
[Route("api/admin/appointments")]
[Authorize(Policy = Policies.Panel)]
public class AdminAppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailQueue _emails;
    private readonly ILogger<AdminAppointmentsController> _logger;
    private readonly IOutputCacheStore _cache;

    /// <summary>Page size when the request does not say, and the maximum accepted.</summary>
    public const int DefaultPageSize = 500;
    public const int MaxPageSize = 2000;

    public AdminAppointmentsController(AppDbContext db, IEmailQueue emails, ILogger<AdminAppointmentsController> logger, IOutputCacheStore cache)
    {
        _db = db;
        _emails = emails;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Appointments of the panel, paginated (page from 1, pageSize up to 2000; 500 by default).
    /// The body is the page; the totals of the whole filter go in headers, so the panel can show the
    /// indicators and "página X de Y": X-Total-Count (all), X-Booked-Count (booked ones).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AppointmentAdminDto>>> List(
        [FromQuery] int? teacherId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] AppointmentStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
    {
        if (!TryGetScope(out var ownTeacherId, out var denied))
        {
            return denied!;
        }

        var query = _db.Appointments.AsNoTracking().AsQueryable();

        if (ownTeacherId is not null)
        {
            // A teacher can never see another teacher's schedule, whatever they ask for in the query string.
            query = query.Where(a => a.TeacherId == ownTeacherId);
        }
        else if (teacherId is not null)
        {
            query = query.Where(a => a.TeacherId == teacherId);
        }

        if (from is not null) query = query.Where(a => a.Date >= from);
        if (to is not null) query = query.Where(a => a.Date <= to);
        if (status is not null) query = query.Where(a => a.Status == status);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        Response.Headers["X-Total-Count"] = (await query.CountAsync()).ToString();
        Response.Headers["X-Booked-Count"] = (await query.CountAsync(a => a.Status == AppointmentStatus.Booked)).ToString();

        var appointments = await query
            .OrderBy(a => a.Date).ThenBy(a => a.StartTime).ThenBy(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AppointmentAdminDto(
                a.Id, a.TeacherId, a.Teacher!.FirstName + " " + a.Teacher!.LastName, a.Date, a.StartTime, a.EndTime,
                a.Status, a.GuardianName, a.GuardianPhone, a.ChildName, a.Notes,
                a.ConfirmationSent, a.ReminderSent, a.Mode, a.ConfirmationRequestedAt, a.GuardianEmail, a.Location))
            .ToListAsync();

        return Ok(appointments);
    }

    /// <summary>
    /// Cancels an appointment from the panel and frees it as available so another family can book it.
    /// The change is saved first; only then the family is notified by email (in the background).
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetScope(out var ownTeacherId, out var denied))
        {
            return denied!;
        }

        var appointment = await _db.Appointments.Include(a => a.Teacher).FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.AppointmentNotFound, "El turno no existe.");
        }

        if (ownTeacherId is not null && appointment.TeacherId != ownTeacherId)
        {
            return Forbidden();
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return this.ApiError(StatusCodes.Status409Conflict, ErrorCodes.AppointmentNotBooked, "Este turno no está reservado: no hay nada para cancelar.");
        }

        var snapshot = appointment.SnapshotForEmail();

        // Status and BookedAt are concurrency tokens: if the family cancelled or the slot changed
        // in the meantime, SaveChanges throws and the panel gets a 409 instead of a silent overwrite.
        // The history row is saved in the same SaveChanges (one transaction).
        _db.AppointmentEvents.Add(AppointmentEvent.From(appointment, AppointmentEventType.CancelledByStaff, CurrentUserIdNumber(), CurrentUserName()));
        appointment.ClearBooking();
        await _db.SaveChangesAsync();
        await _cache.EvictByTagAsync(CachePolicies.AvailabilityTag, default);

        _emails.EnqueueCancellation(snapshot);
        _logger.LogInformation("Appointment {AppointmentId} cancelled from the panel by user {UserId}", id, CurrentUserId());

        return NoContent();
    }

    /// <summary>
    /// Records that the family was asked to confirm attendance. The message itself is sent
    /// from the WhatsApp of whoever presses the button (wa.me link built in the frontend), so
    /// here we only store when it was requested, so the list shows who has already been contacted.
    /// </summary>
    [HttpPost("{id:int}/request-confirmation")]
    public async Task<IActionResult> RequestConfirmation(int id)
    {
        if (!TryGetScope(out var ownTeacherId, out var denied))
        {
            return denied!;
        }

        var appointment = await _db.Appointments.Include(a => a.Teacher).FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.AppointmentNotFound, "El turno no existe.");
        }

        if (ownTeacherId is not null && appointment.TeacherId != ownTeacherId)
        {
            return Forbidden();
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.AppointmentNotBooked, "Solo se puede pedir confirmación de turnos reservados.");
        }

        appointment.ConfirmationRequestedAt = DateTime.UtcNow;
        _db.AppointmentEvents.Add(AppointmentEvent.From(appointment, AppointmentEventType.ConfirmationRequested, CurrentUserIdNumber(), CurrentUserName()));
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// History of bookings, cancellations and confirmation requests (H21), newest first, paginated like the list.
    /// Filters by the date of the appointment (not the date of the event). A Teacher only sees her own.
    /// Total in the X-Total-Count header.
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<AppointmentEventDto>>> History(
        [FromQuery] int? teacherId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] AppointmentEventType? type,
        [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
    {
        if (!TryGetScope(out var ownTeacherId, out var denied))
        {
            return denied!;
        }

        var query = _db.AppointmentEvents.AsNoTracking().AsQueryable();

        if (ownTeacherId is not null)
        {
            query = query.Where(e => e.TeacherId == ownTeacherId);
        }
        else if (teacherId is not null)
        {
            query = query.Where(e => e.TeacherId == teacherId);
        }

        if (from is not null) query = query.Where(e => e.Date >= from);
        if (to is not null) query = query.Where(e => e.Date <= to);
        if (type is not null) query = query.Where(e => e.Type == type);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        Response.Headers["X-Total-Count"] = (await query.CountAsync()).ToString();

        var events = await query
            .OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AppointmentEventDto(
                e.Id, e.AppointmentId, e.TeacherId, e.TeacherName, e.Date, e.StartTime, e.Type, e.Mode,
                e.GuardianName, e.GuardianPhone, e.ChildName, e.UserName, e.CreatedAt))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Deletes the appointments whose ids are listed (the ones the coordinator saw on screen), but only
    /// those still "Available". Booked ones are never deleted here: they are skipped and reported, and
    /// must be cancelled one by one (which notifies the family). Because the delete re-checks the status
    /// in the same statement, a booking made between the search and the click is not lost.
    /// Management only (Admin / Coordinator).
    /// </summary>
    [HttpPost("bulk-delete")]
    [Authorize(Policy = Policies.Management)]
    public async Task<ActionResult<object>> DeleteMany(BulkDeleteAppointmentsDto dto)
    {
        var ids = dto.Ids.Distinct().ToList();

        var deleted = await _db.Appointments
            .Where(a => ids.Contains(a.Id) && a.Status == AppointmentStatus.Available)
            .ExecuteDeleteAsync();

        var skippedBooked = await _db.Appointments
            .CountAsync(a => ids.Contains(a.Id) && a.Status == AppointmentStatus.Booked);
        await _cache.EvictByTagAsync(CachePolicies.AvailabilityTag, default);

        _logger.LogInformation("Bulk delete by user {UserId}: {Deleted} deleted, {Skipped} booked skipped", CurrentUserId(), deleted, skippedBooked);
        return Ok(new { deleted, skippedBooked });
    }

    /// <summary>Deletes an "Available" slot nobody has booked yet (e.g. to fix a data entry mistake).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetScope(out var ownTeacherId, out var denied))
        {
            return denied!;
        }

        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.AppointmentNotFound, "El turno no existe.");
        }

        if (ownTeacherId is not null && appointment.TeacherId != ownTeacherId)
        {
            return Forbidden();
        }

        if (appointment.Status != AppointmentStatus.Available)
        {
            return this.ApiError(StatusCodes.Status409Conflict, ErrorCodes.AppointmentAlreadyBooked, "Solo se pueden eliminar turnos que todavía no fueron reservados. Si ya tiene una familia asignada, cancelalo en vez de eliminarlo.");
        }

        // Status is a concurrency token: if a family books it right now, the DELETE affects 0 rows → 409.
        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();
        await _cache.EvictByTagAsync(CachePolicies.AvailabilityTag, default);
        return NoContent();
    }

    /// <summary>
    /// Decides what the current user may see. Admin/Coordinator → no filter (teacherId = null).
    /// Teacher with a valid teacherId → only that schedule. Anything else → 403 (fail closed).
    /// </summary>
    private bool TryGetScope(out int? teacherId, out ActionResult? denied)
    {
        teacherId = null;
        denied = null;

        if (User.IsInRole(nameof(UserRole.Admin)) || User.IsInRole(nameof(UserRole.Coordinator)))
        {
            return true;
        }

        if (User.IsInRole(nameof(UserRole.Teacher))
            && int.TryParse(User.FindFirstValue(JwtService.TeacherIdClaim), out var id)
            && id > 0)
        {
            teacherId = id;
            return true;
        }

        denied = Forbidden();
        return false;
    }

    private ObjectResult Forbidden() =>
        this.ApiError(StatusCodes.Status403Forbidden, ErrorCodes.Forbidden, "No tenés permiso para ver o modificar este turno.");

    private string? CurrentUserId() => User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

    private int? CurrentUserIdNumber() => int.TryParse(CurrentUserId(), out var id) ? id : null;

    private string? CurrentUserName() => User.FindFirstValue(ClaimTypes.Name);
}
