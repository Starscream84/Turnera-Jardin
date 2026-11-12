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

    /// <summary>
    /// Not used: a cancelled appointment goes back to Available so another family can book the slot,
    /// and the cancellation is recorded in <see cref="Models.AppointmentEvent"/> (history).
    /// The value is kept so the stored numbers never change meaning.
    /// </summary>
    Cancelled = 2,

    /// <summary>Not used (reserved). Kept so the stored numbers never change meaning.</summary>
    Completed = 3
}
