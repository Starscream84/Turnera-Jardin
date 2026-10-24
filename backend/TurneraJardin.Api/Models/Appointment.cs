using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Models;

/// <summary>
/// Represents both an available time slot and, once booked, the actual interview
/// between a teacher and a family. It is modeled as a single entity for simplicity:
/// the admin/teacher "opens" appointments (Status = Available) and a parent books them
/// by filling in the contact details.
/// </summary>
public class Appointment
{
    public int Id { get; set; }

    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Available;

    // Data filled in when booking
    /// <summary>First name of the guardian (parent) who booked.</summary>
    public string? GuardianFirstName { get; set; }

    /// <summary>Last name of the guardian (parent) who booked.</summary>
    public string? GuardianLastName { get; set; }

    /// <summary>Phone number in international E.164 format without "+" (e.g. 5492235551234), ready for the WhatsApp Cloud API.</summary>
    public string? GuardianPhone { get; set; }

    /// <summary>Email address the booking confirmation is sent to.</summary>
    public string? GuardianEmail { get; set; }

    /// <summary>Child's first name.</summary>
    public string? ChildFirstName { get; set; }

    /// <summary>Child's last name.</summary>
    public string? ChildLastName { get; set; }

    public string? Notes { get; set; }

    /// <summary>In-person or online. Stays null while the appointment is available (nobody has chosen yet).</summary>
    public InterviewMode? Mode { get; set; }

    public DateTime? BookedAt { get; set; }

    /// <summary>Set to true when the booking confirmation email was sent.</summary>
    public bool ConfirmationSent { get; set; } = false;

    /// <summary>Set to true when the reminder job already sent the WhatsApp message the day before.</summary>
    public bool ReminderSent { get; set; } = false;

    /// <summary>
    /// Moment (UTC) when the teacher pressed "Confirmación" in her list and WhatsApp was opened
    /// asking the family to confirm attendance. Null if it has not been requested yet.
    /// </summary>
    public DateTime? ConfirmationRequestedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
