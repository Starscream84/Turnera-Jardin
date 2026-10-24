using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface IWhatsAppService
{
    /// <summary>Manda el WhatsApp de confirmación apenas se reserva un turno. Devuelve true si se envió (o simuló) sin errores.</summary>
    Task<bool> EnviarConfirmacionAsync(Turno turno, CancellationToken ct = default);

    /// <summary>Manda el WhatsApp de recordatorio, pensado para correr un día antes del turno.</summary>
    Task<bool> EnviarRecordatorioAsync(Turno turno, CancellationToken ct = default);
}
