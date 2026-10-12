namespace TurneraJardin.Api.Services;

public class RecordatorioBackgroundService : BackgroundService
{
    private readonly ILogger<RecordatorioBackgroundService> _logger;

    public RecordatorioBackgroundService(ILogger<RecordatorioBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de recordatorios iniciado");
        await Task.CompletedTask;
    }
}
