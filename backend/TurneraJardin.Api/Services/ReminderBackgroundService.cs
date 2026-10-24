using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Runs in the background while the API is up. Every so often it checks whether there are
/// booked appointments for "tomorrow" that have not been sent a reminder yet, and sends it via WhatsApp.
///
/// Note: this only works while the API process is running. For a real production deployment
/// without downtime, it should eventually move to a scheduled job (e.g. Hangfire or an OS task),
/// but for this project it is enough and needs no extra infrastructure.
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    public ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started. Checking every {Minutes} minutes.", CheckInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendPendingRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running the reminder check");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // The app is shutting down, this is expected.
            }
        }
    }

    private async Task SendPendingRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var pending = await db.Appointments
            .Include(a => a.Teacher)
            .Where(a => a.Status == AppointmentStatus.Booked
                        && a.Date == tomorrow
                        && !a.ReminderSent)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Sending {Count} reminder(s) for tomorrow's appointments ({Date})", pending.Count, tomorrow);

        foreach (var appointment in pending)
        {
            var sent = await whatsApp.SendReminderAsync(appointment, ct);
            if (sent)
            {
                appointment.ReminderSent = true;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
