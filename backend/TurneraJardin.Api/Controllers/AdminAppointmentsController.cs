using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public AdminAppointmentsController(AppDbContext db, IEmailQueue emails, ILogger<AdminAppointmentsController> logger)
    {
        _db = db;
        _emails = emails;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AppointmentAdminDto>>> List(
        [FromQuery] int? teacherId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] AppointmentStatus? status)
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

        var appointments = await query
            .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
            .Select(a => new AppointmentAdminDto(
                a.Id, a.TeacherId, a.Teacher!.FirstName + " " + a.Teacher!.LastName, a.Date, a.StartTime, a.EndTime,
                a.Status, a.GuardianFirstName, a.GuardianLastName, a.GuardianPhone, a.ChildFirstName, a.ChildLastName, a.Notes,
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
            return NotFound(new { message = "El turno no existe." });
        }

        if (ownTeacherId is not null && appointment.TeacherId != ownTeacherId)
        {
            return Forbidden();
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return Conflict(new { message = "Este turno no está reservado: no hay nada para cancelar." });
        }

        var snapshot = appointment.SnapshotForEmail();

        // Status and BookedAt are concurrency tokens: if the family cancelled or the slot changed
        // in the meantime, SaveChanges throws and the panel gets a 409 instead of a silent overwrite.
        appointment.ClearBooking();
        await _db.SaveChangesAsync();

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

        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound(new { message = "El turno no existe." });
        }

        if (ownTeacherId is not null && appointment.TeacherId != ownTeacherId)
        {
            return Forbidden();
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return BadRequest(new { message = "Solo se puede pedir confirmación de turnos reservados." });
        }

        appointment.ConfirmationRequestedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
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
            return NotFound(new { message = "El turno no existe." });
        }

        if (ownTeacherId is not null && appointment.TeacherId != ownTeacherId)
        {
            return Forbidden();
        }

        if (appointment.Status != AppointmentStatus.Available)
        {
            return Conflict(new { message = "Solo se pueden eliminar turnos que todavía no fueron reservados. Si ya tiene una familia asignada, cancelalo en vez de eliminarlo." });
        }

        // Status is a concurrency token: if a family books it right now, the DELETE affects 0 rows → 409.
        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();
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
        StatusCode(StatusCodes.Status403Forbidden, new { message = "No tenés permiso para ver o modificar este turno." });

    private string? CurrentUserId() => User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
}
