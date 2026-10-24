using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface IWhatsAppService
{
    /// <summary>Sends the WhatsApp confirmation right after an appointment is booked. Returns true if it was sent (or simulated) without errors.</summary>
    Task<bool> SendConfirmationAsync(Appointment appointment, CancellationToken ct = default);

    /// <summary>Sends the WhatsApp reminder, meant to run one day before the appointment.</summary>
    Task<bool> SendReminderAsync(Appointment appointment, CancellationToken ct = default);
}
