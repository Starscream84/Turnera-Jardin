using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Corre en segundo plano mientras la API está levantada. Cada cierto intervalo revisa
/// si hay turnos reservados para "mañana" a los que todavía no se les mandó el recordatorio,
/// y se lo manda por WhatsApp.
///
/// Nota: esto funciona mientras el proceso de la API esté corriendo. Para un despliegue real
/// en producción sin caídas, lo ideal a futuro es moverlo a un job programado (ej. Hangfire o
/// una tarea del sistema operativo), pero para este proyecto alcanza y no requiere infraestructura extra.
/// </summary>
public class RecordatorioBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecordatorioBackgroundService> _logger;
    private static readonly TimeSpan IntervaloChequeo = TimeSpan.FromMinutes(30);

    public RecordatorioBackgroundService(IServiceScopeFactory scopeFactory, ILogger<RecordatorioBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecordatorioBackgroundService iniciado. Verifica cada {Minutos} minutos.", IntervaloChequeo.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnviarRecordatoriosPendientesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error corriendo el chequeo de recordatorios");
            }

            try
            {
                await Task.Delay(IntervaloChequeo, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // La app se está cerrando, es esperable.
            }
        }
    }

    private async Task EnviarRecordatoriosPendientesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var manana = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var turnosPendientes = await db.Turnos
            .Include(t => t.Docente)
            .Where(t => t.Estado == EstadoTurno.Reservado
                        && t.Fecha == manana
                        && !t.RecordatorioEnviado)
            .ToListAsync(ct);

        if (turnosPendientes.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Enviando {Cantidad} recordatorio(s) para turnos de mañana ({Fecha})", turnosPendientes.Count, manana);

        foreach (var turno in turnosPendientes)
        {
            var enviado = await whatsApp.EnviarRecordatorioAsync(turno, ct);
            if (enviado)
            {
                turno.RecordatorioEnviado = true;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
