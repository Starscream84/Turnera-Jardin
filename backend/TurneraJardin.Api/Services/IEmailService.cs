using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface IEmailService
{
    /// <summary>Sends the confirmation email right after an appointment is booked. Returns true if it was sent (or simulated) without errors.</summary>
    Task<bool> SendConfirmationAsync(Appointment appointment, CancellationToken ct = default);

    /// <summary>Notifies the family that the school cancelled their appointment. Returns true if it was sent (or simulated) without errors.</summary>
    Task<bool> SendCancellationAsync(Appointment appointment, CancellationToken ct = default);
}
