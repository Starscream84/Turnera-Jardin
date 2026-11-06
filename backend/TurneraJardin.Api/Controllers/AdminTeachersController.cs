using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>Teacher CRUD. Management only (Admin / Coordinator).</summary>
[ApiController]
[Route("api/admin/teachers")]
[Authorize(Policy = Policies.Management)]
public class AdminTeachersController : ControllerBase
{
    /// <summary>Upper limit of slots created by a single request (one teacher, one time range).</summary>
    public const int MaxAppointmentsPerRequest = 5000;

    /// <summary>How far apart "from" and "to" can be in a single generation.</summary>
    public const int MaxGenerationMonths = 6;

    private readonly AppDbContext _db;

    public AdminTeachersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherAdminDto>>> List()
    {
        var teachers = await _db.Teachers
            .AsNoTracking()
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .Select(t => new TeacherAdminDto(t.Id, t.FirstName, t.LastName, t.Email, t.Classroom, t.IsActive, t.Cycle))
            .ToListAsync();

        return Ok(teachers);
    }

    [HttpPost]
    public async Task<ActionResult<TeacherAdminDto>> Create(TeacherCreateDto dto)
    {
        var email = dto.Email.Trim();
        // The column uses MySQL's case-insensitive collation, so a direct comparison uses the unique index.
        if (await _db.Teachers.AnyAsync(t => t.Email == email))
        {
            return Conflict(new { message = "Ya existe un docente con ese email." });
        }

        var teacher = new Teacher
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            Classroom = dto.Classroom?.Trim(),
            Cycle = dto.Cycle,
            IsActive = true
        };

        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync(); // a concurrent duplicate → MySQL 1062 → 409 (ApiExceptionHandler)

        var result = new TeacherAdminDto(teacher.Id, teacher.FirstName, teacher.LastName, teacher.Email, teacher.Classroom, teacher.IsActive, teacher.Cycle);
        return Created($"/api/admin/teachers/{teacher.Id}", result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TeacherUpdateDto dto)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null)
        {
            return NotFound(new { message = "Docente no encontrado." });
        }

        var email = dto.Email.Trim();
        if (await _db.Teachers.AnyAsync(t => t.Email == email && t.Id != id))
        {
            return Conflict(new { message = "Ya existe otro docente con ese email." });
        }

        teacher.FirstName = dto.FirstName.Trim();
        teacher.LastName = dto.LastName.Trim();
        teacher.Email = email;
        teacher.Classroom = dto.Classroom?.Trim();
        teacher.Cycle = dto.Cycle;
        teacher.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Does not physically delete the teacher (to keep the history of past appointments):
    /// it deactivates them, which removes them from the public schedule.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null)
        {
            return NotFound(new { message = "Docente no encontrado." });
        }

        teacher.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Bulk-generates "Available" appointments for a teacher, by days of the week and time range.</summary>
    [HttpPost("{id:int}/generate-appointments")]
    public async Task<ActionResult<object>> GenerateAppointments(int id, GenerateAppointmentsDto dto)
    {
        var teacher = await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (teacher is null)
        {
            return NotFound(new { message = "Docente no encontrado." });
        }

        if (!teacher.IsActive)
        {
            return BadRequest(new { message = "El docente está desactivado: activalo antes de generarle turnos." });
        }

        if (dto.ToDate < dto.FromDate)
        {
            return BadRequest(new { message = "La fecha 'hasta' no puede ser anterior a la fecha 'desde'." });
        }

        if (dto.FromDate < ArgentinaDate.Today())
        {
            return BadRequest(new { message = "La fecha 'desde' no puede ser anterior a hoy." });
        }

        if (dto.ToDate > dto.FromDate.AddMonths(MaxGenerationMonths))
        {
            return BadRequest(new { message = $"Se pueden generar turnos para hasta {MaxGenerationMonths} meses por vez." });
        }

        if (!InterviewLocations.IsValid(dto.Location))
        {
            return BadRequest(new { message = "Elegí una de las direcciones de la lista para la entrevista." });
        }

        var slots = AppointmentSlots.ForDay(dto.StartTime, dto.EndTime, dto.DurationMinutes);
        if (slots is null)
        {
            return BadRequest(new { message = "La hora de fin debe ser posterior a la hora de inicio, y tiene que entrar al menos un turno completo en la franja." });
        }

        var dates = new List<DateOnly>();
        for (var date = dto.FromDate; date <= dto.ToDate; date = date.AddDays(1))
        {
            if (dto.DaysOfWeek.Contains(date.DayOfWeek))
            {
                dates.Add(date);
            }
        }

        if ((long)dates.Count * slots.Count > MaxAppointmentsPerRequest)
        {
            return BadRequest(new { message = $"Son demasiados turnos para un solo pedido (más de {MaxAppointmentsPerRequest}). Achicá el rango de fechas o alargá la duración." });
        }

        var location = dto.Location.Trim();

        // Two attempts: if another coordinator generated the same slots at the same moment, the unique index
        // (TeacherId, Date, StartTime) rejects ours; we re-read what exists and insert only what's missing.
        for (var attempt = 1; ; attempt++)
        {
            var existing = await _db.Appointments
                .Where(a => a.TeacherId == id && a.Date >= dto.FromDate && a.Date <= dto.ToDate)
                .Select(a => new { a.Date, a.StartTime })
                .ToListAsync();
            var existingSet = existing.Select(e => (e.Date, e.StartTime)).ToHashSet();

            var newAppointments = new List<Appointment>();
            foreach (var date in dates)
            {
                foreach (var (start, end) in slots)
                {
                    if (existingSet.Contains((date, start)))
                    {
                        continue; // there is already an appointment at that time, don't duplicate it
                    }

                    newAppointments.Add(new Appointment
                    {
                        TeacherId = id,
                        Date = date,
                        StartTime = start,
                        EndTime = end,
                        Status = AppointmentStatus.Available,
                        Location = location
                    });
                }
            }

            try
            {
                _db.Appointments.AddRange(newAppointments);
                await _db.SaveChangesAsync();
                return Ok(new { created = newAppointments.Count, skippedExisting = dates.Count * slots.Count - newAppointments.Count });
            }
            catch (DbUpdateException ex) when (attempt == 1 && ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                _db.ChangeTracker.Clear();
            }
        }
    }
}

/// <summary>Splits a time range into slots. Pure function (no database), covered by unit tests.</summary>
public static class AppointmentSlots
{
    /// <summary>
    /// Slots of <paramref name="durationMinutes"/> from start to end, computed in whole minutes so they
    /// can never wrap past midnight (TimeOnly.Add wraps 23:00 + 60 min to 00:00, which caused an infinite loop).
    /// Returns null if the range is invalid or not even one slot fits.
    /// </summary>
    public static List<(TimeOnly Start, TimeOnly End)>? ForDay(TimeOnly start, TimeOnly end, int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            return null;
        }

        var startMinute = start.Hour * 60 + start.Minute;
        var endMinute = end.Hour * 60 + end.Minute;
        if (endMinute <= startMinute)
        {
            return null;
        }

        var slots = new List<(TimeOnly, TimeOnly)>();
        for (var minute = startMinute; minute + durationMinutes <= endMinute; minute += durationMinutes)
        {
            slots.Add((TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minute)),
                       TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minute + durationMinutes))));
        }

        return slots.Count == 0 ? null : slots;
    }
}
