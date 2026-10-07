using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface IEmailService
{
    /// <summary>Manda el email de confirmación apenas se reserva un turno. Devuelve true si se envió (o simuló) sin errores.</summary>
    Task<bool> EnviarConfirmacionAsync(Turno turno, CancellationToken ct = default);
}
