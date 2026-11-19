using System.ComponentModel.DataAnnotations;
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

    /// <summary>
    /// Concurrency token (together with <see cref="BookedAt"/>): every UPDATE made through SaveChanges
    /// includes "WHERE Status = original value", so if someone else booked, cancelled or deleted the
    /// appointment in the meantime, EF throws DbUpdateConcurrencyException (answered as 409) instead
    /// of silently overwriting the other change.
    /// </summary>
    [ConcurrencyCheck]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Available;

    /// <summary>
    /// Address of the in-person interview, chosen by the coordinator when generating the appointment
    /// (one of <see cref="InterviewLocations.All"/>). Only shown to the family if the interview is in person.
    /// Null for appointments generated before this field existed.
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    // Data filled in when booking
    /// <summary>Full name (first and last name, in a single field) of the guardian (parent) who booked.</summary>
    [MaxLength(200)]
    public string? GuardianName { get; set; }

    /// <summary>Phone number in international E.164 format without "+" (e.g. 5492235551234), ready for the WhatsApp Cloud API.</summary>
    public string? GuardianPhone { get; set; }

    /// <summary>Email address the booking confirmation is sent to.</summary>
    public string? GuardianEmail { get; set; }

    /// <summary>Child's full name (first and last name, in a single field).</summary>
    [MaxLength(200)]
    public string? ChildName { get; set; }

    public string? Notes { get; set; }

    /// <summary>In-person or online. Stays null while the appointment is available (nobody has chosen yet).</summary>
    public InterviewMode? Mode { get; set; }

    /// <summary>Moment (UTC) of the booking. Also a concurrency token: it changes on every new booking.</summary>
    [ConcurrencyCheck]
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

    /// <summary>Frees the slot: back to Available and without any of the family's data.</summary>
    public void ClearBooking()
    {
        Status = AppointmentStatus.Available;
        GuardianName = null;
        GuardianPhone = null;
        GuardianEmail = null;
        ChildName = null;
        Notes = null;
        Mode = null;
        BookedAt = null;
        ConfirmationSent = false;
        ReminderSent = false;
        ConfirmationRequestedAt = null;
    }

    /// <summary>Copy of the booking data, to email the family after the slot was already cleared.</summary>
    public Appointment SnapshotForEmail() => (Appointment)MemberwiseClone();
}
