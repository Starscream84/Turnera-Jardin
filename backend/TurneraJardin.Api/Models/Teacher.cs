using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Models;

/// <summary>
/// A kindergarten teacher, each one with their own appointment schedule.
/// </summary>
public class Teacher
{
    public int Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Email { get; set; }

    /// <summary>Classroom or group in charge (e.g. "Sala Celeste", "Sala de 2 años"). Informational only.</summary>
    public string? Classroom { get; set; }

    /// <summary>Teacher's cycle. Null = no cycle: the family chooses in-person or online.</summary>
    public TeacherCycle? Cycle { get; set; }

    /// <summary>When false, the teacher is hidden from the public schedule and no new appointments can be generated.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public string FullName => $"{FirstName} {LastName}";
}
