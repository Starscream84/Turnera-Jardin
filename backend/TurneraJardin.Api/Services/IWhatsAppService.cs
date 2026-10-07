using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface IWhatsAppService
{
    Task<bool> EnviarConfirmacionAsync(Turno turno);
}
