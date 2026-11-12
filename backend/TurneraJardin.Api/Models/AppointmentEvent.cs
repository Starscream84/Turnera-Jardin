using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Models;

/// <summary>
/// History of an appointment: one row per booking, cancellation or confirmation request.
/// The slot itself (<see cref="Appointment"/>) is reused when it is cancelled, so this table is what
/// keeps the record of who had booked it and who cancelled it.
///
/// It is a snapshot on purpose: it copies the date, time, teacher and family names instead of relying
/// only on foreign keys, so the history survives if the slot is later deleted or the teacher deactivated.
/// It keeps the family's name and phone (not the email); see docs/migraciones-y-errores.md for retention.
/// </summary>
public class AppointmentEvent
{
    public long Id { get; set; }

    /// <summary>Id of the slot. No foreign key: the slot may be deleted later and the history must remain.</summary>
    public int AppointmentId { get; set; }

    public int TeacherId { get; set; }

    [MaxLength(200)]
    public string TeacherName { get; set; } = "";

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public AppointmentEventType Type { get; set; }

    public InterviewMode? Mode { get; set; }

    [MaxLength(200)]
    public string? GuardianName { get; set; }

    [MaxLength(30)]
    public string? GuardianPhone { get; set; }

    [MaxLength(200)]
    public string? ChildName { get; set; }

    /// <summary>Panel user who did it (null when it was the family from the public page).</summary>
    public int? UserId { get; set; }

    [MaxLength(200)]
    public string? UserName { get; set; }

    /// <summary>Moment of the event (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Builds the event from the current state of a booked appointment (call it before clearing the booking).</summary>
    public static AppointmentEvent From(Appointment appointment, AppointmentEventType type, int? userId = null, string? userName = null) => new()
    {
        AppointmentId = appointment.Id,
        TeacherId = appointment.TeacherId,
        TeacherName = appointment.Teacher?.FullName ?? "",
        Date = appointment.Date,
        StartTime = appointment.StartTime,
        Type = type,
        Mode = appointment.Mode,
        GuardianName = Join(appointment.GuardianFirstName, appointment.GuardianLastName),
        GuardianPhone = appointment.GuardianPhone,
        ChildName = Join(appointment.ChildFirstName, appointment.ChildLastName),
        UserId = userId,
        UserName = userName,
        CreatedAt = DateTime.UtcNow
    };

    private static string? Join(string? first, string? last)
    {
        var full = $"{first} {last}".Trim();
        return full.Length == 0 ? null : full.Length > 200 ? full[..200] : full;
    }
}
