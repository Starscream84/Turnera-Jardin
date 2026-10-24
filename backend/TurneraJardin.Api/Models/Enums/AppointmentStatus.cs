namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Possible states of an appointment throughout its lifecycle.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Time slot opened by the teacher, not booked yet.</summary>
    Available = 0,

    /// <summary>A parent booked the appointment.</summary>
    Booked = 1,

    /// <summary>The appointment was cancelled (by the parent or by the school).</summary>
    Cancelled = 2,

    /// <summary>The interview already took place.</summary>
    Completed = 3
}
