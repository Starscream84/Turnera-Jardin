using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public class WhatsAppCloudApiService : IWhatsAppService
{
    private readonly ILogger<WhatsAppCloudApiService> _logger;

    public WhatsAppCloudApiService(ILogger<WhatsAppCloudApiService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> EnviarConfirmacionAsync(Turno turno)
    {
        _logger.LogInformation("Confirmación para turno {TurnoId}", turno.Id);
        return true;
    }
}
