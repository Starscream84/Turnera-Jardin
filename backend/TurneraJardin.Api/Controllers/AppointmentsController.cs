using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace TurneraJardin.Api.Controllers;

/// <summary>Public endpoints for a parent to book or cancel their appointment. No authentication.</summary>
[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(AppDbContext db, IEmailService email, ILogger<AppointmentsController> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<AvailableAppointmentDto>>> GetAvailable(
        [FromQuery] int? teacherId,
        [FromQuery] DateOnly? date)
    {
        var query = _db.Appointments
            .Where(a => a.Status == AppointmentStatus.Available);

        if (teacherId.HasValue)
            query = query.Where(a => a.TeacherId == teacherId.Value);

        if (date != default)
            query = query.Where(a => a.Date == date);

        var appointments = await query
            .Include(a => a.Teacher)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToListAsync();

        var dtos = appointments.Select(a => new AvailableAppointmentDto(
            a.Id,
            a.TeacherId,
            a.Teacher!.FullName,
            a.Teacher!.Classroom ?? "Sin asignar",
            a.Date,
            a.StartTime,
            a.EndTime,
            a.Status.ToString()
        )).ToList();

        return Ok(dtos);
    }

    [EnableRateLimiting("public")]
    [HttpPost("{id:int}/book")]
    public async Task<ActionResult<BookedAppointmentDto>> Book(int id, BookAppointmentDto dto)
    {
        // A transaction prevents two parents from booking the same appointment at the same time.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var appointment = await _db.Appointments.Include(a => a.Teacher).FirstOrDefaultAsync(a => a.Id == id);

        if (appointment is null)
        {
            return NotFound(new { message = "El turno no existe." });
        }

        if (appointment.Status != AppointmentStatus.Available)
        {
            return Conflict(new { message = "Este turno ya no está disponible. Elegí otro horario." });
        }

        if (appointment.Date < ArgentinaDate.FirstBookableDay())
        {
            return BadRequest(new { message = "Los turnos se reservan con al menos un día de anticipación. Elegí un turno a partir de mañana." });
        }

        // First cycle and specialists: interviews are always in person and the family does not choose.
        if (appointment.Teacher!.Cycle is TeacherCycle.FirstCycle or TeacherCycle.Specialist)
        {
            if (dto.Mode == InterviewMode.Online)
            {
                return BadRequest(new { message = "Esta docente solo atiende entrevistas presenciales." });
            }

            dto.Mode = InterviewMode.InPerson;
        }

        appointment.GuardianFirstName = dto.GuardianFirstName.Trim();
        appointment.GuardianLastName = dto.GuardianLastName.Trim();
        appointment.GuardianPhone = dto.GuardianPhone.Trim();
        appointment.GuardianEmail = dto.GuardianEmail.Trim();
        appointment.ChildFirstName = dto.ChildFirstName.Trim();
        appointment.ChildLastName = dto.ChildLastName.Trim();
        appointment.Notes = dto.Notes?.Trim();
        appointment.Mode = dto.Mode;
        appointment.Status = AppointmentStatus.Booked;
        appointment.BookedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var sent = await _email.SendConfirmationAsync(appointment);
        appointment.ConfirmationSent = sent;
        await _db.SaveChangesAsync();

        if (!sent)
        {
            _logger.LogWarning("Appointment {AppointmentId} was booked but the confirmation email could not be sent.", appointment.Id);
        }

        return Ok(new BookedAppointmentDto(
            appointment.Id,
            appointment.Teacher!.FullName,
            appointment.Date,
            appointment.StartTime,
            appointment.EndTime,
            appointment.ChildFirstName!,
            appointment.ChildLastName!,
            appointment.Mode!.Value));
    }

    /// <summary>
    /// Cancels a booked appointment and frees it as available so another family can book it.
    /// As basic security, the same phone number used when booking is required.
    /// </summary>
    [EnableRateLimiting("public")]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string phone)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);

        if (appointment is null || appointment.Status != AppointmentStatus.Booked)
        {
            return NotFound(new { message = "No se encontró un turno reservado con ese id." });
        }

        var enteredDigits = new string(phone.Where(char.IsDigit).ToArray());
        var storedDigits = new string((appointment.GuardianPhone ?? "").Where(char.IsDigit).ToArray());

        if (enteredDigits != storedDigits)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "El teléfono no coincide con el de la reserva." });
        }

        // Free the appointment so another family can book it
        appointment.Status = AppointmentStatus.Available;

        // Clear all booking data so the slot becomes available again
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
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
