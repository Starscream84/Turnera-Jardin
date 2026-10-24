using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>Public endpoints: choose a teacher and see their availability. No authentication.</summary>
[ApiController]
[Route("api/teachers")]
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

    /// <summary>Free appointments of a teacher within a date range (by default, from tomorrow and the following 30 days).</summary>
    [HttpGet("{id:int}/available-appointments")]
    public async Task<ActionResult<List<AvailableAppointmentDto>>> AvailableAppointments(
        int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var teacherExists = await _db.Teachers.AnyAsync(t => t.Id == id && t.IsActive);
        if (!teacherExists)
        {
            return NotFound(new { message = "Docente no encontrado." });
        }

        var firstDay = ArgentinaDate.FirstBookableDay();
        var fromDate = from is not null && from >= firstDay ? from.Value : firstDay;
        var toDate = to ?? fromDate.AddDays(30);

        var appointments = await _db.Appointments
            .Where(a => a.TeacherId == id
                        && a.Status == AppointmentStatus.Available
                        && a.Date >= fromDate
                        && a.Date <= toDate)
            .Include(a => a.Teacher)
            .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
            .Select(a => new AvailableAppointmentDto(
                a.Id,
                a.TeacherId,
                a.Teacher!.FullName,
                a.Teacher!.Classroom ?? "Sin asignar",
                a.Date,
                a.StartTime,
                a.EndTime,
                a.Status.ToString()))
            .ToListAsync();

        return Ok(appointments);
    }
}
