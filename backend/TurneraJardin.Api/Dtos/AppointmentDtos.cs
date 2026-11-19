using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Dtos;

/// <summary>Available slot, as a parent sees it before booking.</summary>
public record AvailableAppointmentDto(
    int Id,
    int TeacherId,
    string TeacherName,
    string TeacherClassroom,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    string? Location
);

/// <summary>Data the parent fills in to book an already chosen appointment.</summary>
public class BookAppointmentDto
{
    /// <summary>Guardian's first and last name, in a single field.</summary>
    [Required(ErrorMessage = "Ingresá el nombre y apellido del tutor/a."), MaxLength(200)]
    public required string GuardianName { get; set; }

    /// <summary>WhatsApp number, in any usual Argentine format. Stored normalized (see <see cref="Services.PhoneNumber"/>).</summary>
    [Required(ErrorMessage = "Ingresá tu WhatsApp."), ArgentinePhone, MaxLength(30)]
    public required string GuardianPhone { get; set; }

    /// <summary>Email address the booking confirmation is sent to.</summary>
    [Required(ErrorMessage = "Ingresá un email para recibir la confirmación.")]
    [StrictEmail]
    [MaxLength(200)]
    public required string GuardianEmail { get; set; }

    /// <summary>Child's first and last name, in a single field.</summary>
    [Required(ErrorMessage = "Ingresá el nombre y apellido del niño/a."), MaxLength(200)]
    public required string ChildName { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>"InPerson" or "Online". Nullable only so that, if missing, validation fails instead of assuming InPerson.</summary>
    [Required(ErrorMessage = "Elegí si la entrevista es presencial o virtual.")]
    [EnumDataType(typeof(InterviewMode))]
    public InterviewMode? Mode { get; set; }
}

/// <summary>Data to cancel a booking from the public page. The phone travels in the body, not in the URL.</summary>
public class CancelAppointmentDto
{
    /// <summary>Not [Required] so an older frontend that still sends "{}" plus "?phone=" keeps working.</summary>
    [MaxLength(30)]
    public string? Phone { get; set; }
}

/// <summary>
/// Explicit list of appointments to delete from the panel (the ones the coordinator saw on screen).
/// Only "Available" ones are deleted; booked ones are skipped and must be cancelled one by one,
/// which notifies the family.
/// </summary>
public class BulkDeleteAppointmentsDto
{
    [Required, MinLength(1), MaxLength(5000)]
    public required List<int> Ids { get; set; }
}

/// <summary>Confirmation returned to the parent after a successful booking.</summary>
public record BookedAppointmentDto(
    int Id,
    string TeacherName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ChildName,
    InterviewMode Mode,
    // Interview address. Null if the interview is online.
    string? Location
);

/// <summary>Full view of an appointment for the admin / teacher panel.</summary>
public record AppointmentAdminDto(
    int Id,
    int TeacherId,
    string TeacherName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    AppointmentStatus Status,
    string? GuardianName,
    string? GuardianPhone,
    string? ChildName,
    string? Notes,
    bool ConfirmationSent,
    bool ReminderSent,
    InterviewMode? Mode,
    DateTime? ConfirmationRequestedAt,
    string? GuardianEmail,
    string? Location
);

/// <summary>Request to bulk-generate available appointments for a teacher.</summary>
public class GenerateAppointmentsDto
{
    [Required]
    public DateOnly FromDate { get; set; }

    [Required]
    public DateOnly ToDate { get; set; }

    /// <summary>Days of the week on which appointments are opened (0 = Sunday .. 6 = Saturday).</summary>
    [Required, MinLength(1)]
    public required List<DayOfWeek> DaysOfWeek { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [Range(5, 240)]
    public int DurationMinutes { get; set; } = 20;

    /// <summary>Address of the interview: must be one of <see cref="Models.InterviewLocations.All"/>.</summary>
    [Required(ErrorMessage = "Elegí dónde va a ser la entrevista."), MaxLength(200)]
    public required string Location { get; set; }
}

/// <summary>One row of the appointment history (panel → Historial). UserName is null when the family did it from the web.</summary>
public record AppointmentEventDto(
    long Id,
    int AppointmentId,
    int TeacherId,
    string TeacherName,
    DateOnly Date,
    TimeOnly StartTime,
    AppointmentEventType Type,
    InterviewMode? Mode,
    string? GuardianName,
    string? GuardianPhone,
    string? ChildName,
    string? UserName,
    DateTime CreatedAt);
