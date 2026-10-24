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
    string Status
);

/// <summary>Data the parent fills in to book an already chosen appointment.</summary>
public class BookAppointmentDto
{
    [Required, MaxLength(120)]
    public required string GuardianFirstName { get; set; }

    [Required, MaxLength(120)]
    public required string GuardianLastName { get; set; }

    /// <summary>WhatsApp number. Accepted with or without "+"; the backend normalizes it.</summary>
    [Required, Phone, MaxLength(30)]
    public required string GuardianPhone { get; set; }

    /// <summary>Email address the booking confirmation is sent to.</summary>
    [Required(ErrorMessage = "Ingresá un email para recibir la confirmación.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [MaxLength(200)]
    public required string GuardianEmail { get; set; }

    [Required, MaxLength(120)]
    public required string ChildFirstName { get; set; }

    [Required, MaxLength(120)]
    public required string ChildLastName { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>"InPerson" or "Online". Nullable only so that, if missing, validation fails instead of assuming InPerson.</summary>
    [Required(ErrorMessage = "Elegí si la entrevista es presencial o virtual.")]
    [EnumDataType(typeof(InterviewMode))]
    public InterviewMode? Mode { get; set; }
}

/// <summary>Confirmation returned to the parent after a successful booking.</summary>
public record BookedAppointmentDto(
    int Id,
    string TeacherName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ChildFirstName,
    string ChildLastName,
    InterviewMode Mode
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
    string? GuardianFirstName,
    string? GuardianLastName,
    string? GuardianPhone,
    string? ChildFirstName,
    string? ChildLastName,
    string? Notes,
    bool ConfirmationSent,
    bool ReminderSent,
    InterviewMode? Mode,
    DateTime? ConfirmationRequestedAt,
    string? GuardianEmail
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
}
