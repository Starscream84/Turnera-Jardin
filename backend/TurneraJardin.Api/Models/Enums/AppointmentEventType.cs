namespace TurneraJardin.Api.Models.Enums;

/// <summary>What happened to an appointment, as recorded in its history (<see cref="AppointmentEvent"/>).</summary>
public enum AppointmentEventType
{
    /// <summary>A family booked the slot from the public page.</summary>
    Booked = 0,

    /// <summary>The family cancelled from the public page.</summary>
    CancelledByFamily = 1,

    /// <summary>Someone from the school cancelled from the panel (the family is emailed).</summary>
    CancelledByStaff = 2,

    /// <summary>The teacher pressed "Confirmación" to ask the family to confirm by WhatsApp.</summary>
    ConfirmationRequested = 3
}
