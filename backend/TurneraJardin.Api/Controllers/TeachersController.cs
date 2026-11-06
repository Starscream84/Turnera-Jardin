using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>Public endpoints: choose a teacher and see their availability. No authentication.</summary>
[ApiController]
[Route("api/teachers")]
[EnableRateLimiting(RateLimitPolicies.PublicRead)]
public class TeachersController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeachersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherDto>>> List()
    {
        var teachers = await _db.Teachers
            .Where(t => t.IsActive)
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .Select(t => new TeacherDto(t.Id, t.FirstName + " " + t.LastName, t.Classroom, t.Cycle))
            .ToListAsync();

        return Ok(teachers);
    }

    /// <summary>
    /// Free appointments of a teacher within a date range: by default from tomorrow and the following 30 days,
    /// never before tomorrow nor more than <see cref="ArgentinaDate.BookingDaysAhead"/> days ahead.
    /// </summary>
    [HttpGet("{id:int}/available-appointments")]
    public async Task<ActionResult<List<AvailableAppointmentDto>>> AvailableAppointments(
        int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (from is not null && to is not null && to < from)
        {
            return BadRequest(new { message = "La fecha 'hasta' no puede ser anterior a la fecha 'desde'." });
        }

        var teacherExists = await _db.Teachers.AnyAsync(t => t.Id == id && t.IsActive);
        if (!teacherExists)
        {
            return NotFound(new { message = "Docente no encontrado." });
        }

        var firstDay = ArgentinaDate.FirstBookableDay();
        var lastDay = ArgentinaDate.LastBookableDay();
        var fromDate = from is not null && from >= firstDay ? from.Value : firstDay;
        var toDate = to ?? fromDate.AddDays(30);
        if (toDate > lastDay)
        {
            toDate = lastDay;
        }

        var appointments = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.TeacherId == id
                        && a.Status == AppointmentStatus.Available
                        && a.Date >= fromDate
                        && a.Date <= toDate)
            .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
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

        return Ok(appointments);
    }
}
