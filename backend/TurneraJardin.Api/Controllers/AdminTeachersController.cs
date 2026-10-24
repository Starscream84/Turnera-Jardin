using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Controllers;

/// <summary>Teacher CRUD. Management only (Admin / Coordinator).</summary>
[ApiController]
[Route("api/admin/teachers")]
[Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Coordinator)}")]
public class AdminTeachersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminTeachersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherAdminDto>>> List()
    {
        var teachers = await _db.Teachers
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .Select(t => new TeacherAdminDto(t.Id, t.FirstName, t.LastName, t.Email, t.Classroom, t.IsActive, t.Cycle))
            .ToListAsync();

        return Ok(teachers);
    }

    [HttpPost]
    public async Task<ActionResult<TeacherAdminDto>> Create(TeacherCreateDto dto)
    {
        var alreadyExists = await _db.Teachers.AnyAsync(t => t.Email.ToLower() == dto.Email.ToLower());
        if (alreadyExists)
        {
            return Conflict(new { message = "Ya existe un docente con ese email." });
        }

        var teacher = new Teacher
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Classroom = dto.Classroom?.Trim(),
            Cycle = dto.Cycle,
            IsActive = true
        };

        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var result = new TeacherAdminDto(teacher.Id, teacher.FirstName, teacher.LastName, teacher.Email, teacher.Classroom, teacher.IsActive, teacher.Cycle);
        return CreatedAtAction(nameof(List), new { id = teacher.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TeacherUpdateDto dto)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null)
        {
            return NotFound();
        }

        teacher.FirstName = dto.FirstName.Trim();
        teacher.LastName = dto.LastName.Trim();
        teacher.Email = dto.Email.Trim();
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
            return NotFound();
        }

        teacher.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Bulk-generates "Available" appointments for a teacher, by days of the week and time range.</summary>
    [HttpPost("{id:int}/generate-appointments")]
    public async Task<ActionResult<object>> GenerateAppointments(int id, GenerateAppointmentsDto dto)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null)
        {
            return NotFound(new { message = "Docente no encontrado." });
        }

        if (dto.ToDate < dto.FromDate)
        {
            return BadRequest(new { message = "La fecha 'hasta' no puede ser anterior a la fecha 'desde'." });
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return BadRequest(new { message = "La hora de fin debe ser posterior a la hora de inicio." });
        }

        var existing = await _db.Appointments
            .Where(a => a.TeacherId == id && a.Date >= dto.FromDate && a.Date <= dto.ToDate)
            .Select(a => new { a.Date, a.StartTime })
            .ToListAsync();
        var existingSet = existing.Select(e => (e.Date, e.StartTime)).ToHashSet();

        var newAppointments = new List<Appointment>();
        var duration = TimeSpan.FromMinutes(dto.DurationMinutes);

        for (var date = dto.FromDate; date <= dto.ToDate; date = date.AddDays(1))
        {
            if (!dto.DaysOfWeek.Contains(date.DayOfWeek))
            {
                continue;
            }

            for (var startTime = dto.StartTime; startTime.Add(duration) <= dto.EndTime; startTime = startTime.Add(duration))
            {
                if (existingSet.Contains((date, startTime)))
                {
                    continue; // there is already an appointment at that time, don't duplicate it
                }

                newAppointments.Add(new Appointment
                {
                    TeacherId = id,
                    Date = date,
                    StartTime = startTime,
                    EndTime = startTime.Add(duration),
                    Status = AppointmentStatus.Available
                });
            }
        }

        _db.Appointments.AddRange(newAppointments);
        await _db.SaveChangesAsync();

        return Ok(new { created = newAppointments.Count });
    }
}
