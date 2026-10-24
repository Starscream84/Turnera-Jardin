using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>
/// Appointment list and management for the admin panel. An Admin sees everything;
/// a user with the Teacher role can only see/manage the appointments of their own schedule.
/// </summary>
[ApiController]
[Route("api/admin/appointments")]
[Authorize]
public class AdminAppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<AdminAppointmentsController> _logger;

    public AdminAppointmentsController(AppDbContext db, IEmailService email, ILogger<AdminAppointmentsController> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AppointmentAdminDto>>> List(
        [FromQuery] int? teacherId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] AppointmentStatus? status)
    {
        var query = _db.Appointments.Include(a => a.Teacher).AsQueryable();

        if (IsTeacher(out var ownTeacherId))
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
                a.Id, a.TeacherId, a.Teacher!.FullName, a.Date, a.StartTime, a.EndTime,
                a.Status, a.GuardianFirstName, a.GuardianLastName, a.GuardianPhone, a.ChildFirstName, a.ChildLastName, a.Notes,
                a.ConfirmationSent, a.ReminderSent, a.Mode, a.ConfirmationRequestedAt, a.GuardianEmail))
            .ToListAsync();

        return Ok(appointments);
    }

    /// <summary>
    /// Cancels an appointment from the panel and frees it as available so another family can book it.
    /// If the appointment had a family assigned, they are notified by email that their booking was cancelled.
    /// If the email fails, the appointment is freed anyway and the error goes to the log.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var appointment = await _db.Appointments.Include(a => a.Teacher).FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (IsTeacher(out var ownTeacherId) && appointment.TeacherId != ownTeacherId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var wasBooked = appointment.Status == AppointmentStatus.Booked;

        if (wasBooked)
        {
            var sent = await _email.SendCancellationAsync(appointment);
            if (!sent)
            {
                _logger.LogWarning("Appointment {AppointmentId} was cancelled but the notice email to the family could not be sent.", appointment.Id);
            }
        }
        // Free the appointment so another family can book it
        appointment.Status = AppointmentStatus.Available;

        // Clear all booking data so the slot becomes available again
        if (wasBooked)
        {
            appointment.GuardianFirstName = null;
            appointment.GuardianLastName = null;
            appointment.GuardianPhone = null;
            appointment.GuardianEmail = null;
            appointment.ChildFirstName = null;
            appointment.ChildLastName = null;
            appointment.Notes = null;
            appointment.Mode = null;
            appointment.BookedAt = null;
            appointment.ConfirmationSent = false;
            appointment.ReminderSent = false;
            appointment.ConfirmationRequestedAt = null;
        }

        await _db.SaveChangesAsync();

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
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (IsTeacher(out var ownTeacherId) && appointment.TeacherId != ownTeacherId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
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
    /// Bulk-deletes every appointment matching the filter (same parameters as the list).
    /// At least one filter is required. Booked appointments are deleted too: families are not
    /// notified and it cannot be undone. Management only (Admin / Coordinator), never a teacher.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Coordinator)}")]
    public async Task<ActionResult<object>> DeleteMany(
        [FromQuery] int? teacherId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] AppointmentStatus? status)
    {
        if (teacherId is null && from is null && to is null && status is null)
        {
            return BadRequest(new { message = "Indicá al menos un filtro (docente, fechas o estado) para eliminar turnos en bloque." });
        }

        var query = _db.Appointments.AsQueryable();

        if (teacherId is not null) query = query.Where(a => a.TeacherId == teacherId);
        if (from is not null) query = query.Where(a => a.Date >= from);
        if (to is not null) query = query.Where(a => a.Date <= to);
        if (status is not null) query = query.Where(a => a.Status == status);

        var deleted = await query.ExecuteDeleteAsync();
        return Ok(new { deleted });
    }

    /// <summary>Deletes an "Available" slot nobody has booked yet (e.g. to fix a data entry mistake).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (IsTeacher(out var ownTeacherId) && appointment.TeacherId != ownTeacherId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (appointment.Status != AppointmentStatus.Available)
        {
            return BadRequest(new { message = "Solo se pueden eliminar turnos que todavía no fueron reservados. Si ya tiene una familia asignada, cancelalo en vez de eliminarlo." });
        }

        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private bool IsTeacher(out int teacherId)
    {
        teacherId = 0;
        if (!User.IsInRole(nameof(UserRole.Teacher)))
        {
            return false;
        }

        var claim = User.FindFirstValue(JwtService.TeacherIdClaim);
        return claim is not null && int.TryParse(claim, out teacherId);
    }
}
