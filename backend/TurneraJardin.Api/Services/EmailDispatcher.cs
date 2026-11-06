using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Queue of emails to send in the background, so that booking or cancelling never waits for Brevo.
/// The controllers only enqueue; <see cref="EmailDispatcherService"/> does the sending.
/// </summary>
public interface IEmailQueue
{
    /// <summary>Sends the confirmation of a booked appointment (it is re-read from the database before sending).</summary>
    void EnqueueConfirmation(int appointmentId);

    /// <summary>Sends the cancellation notice. Receives a copy of the booking because the slot is already cleared.</summary>
    void EnqueueCancellation(Appointment bookingSnapshot);
}

public sealed record EmailJob(int? ConfirmationAppointmentId, Appointment? CancellationSnapshot);

public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailJob> _channel = Channel.CreateBounded<EmailJob>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest });

    public ChannelReader<EmailJob> Reader => _channel.Reader;

    public void EnqueueConfirmation(int appointmentId) =>
        _channel.Writer.TryWrite(new EmailJob(appointmentId, null));

    public void EnqueueCancellation(Appointment bookingSnapshot) =>
        _channel.Writer.TryWrite(new EmailJob(null, bookingSnapshot));
}

/// <summary>
/// Sends the queued emails one at a time and, every few minutes, retries the confirmations that
/// could not be sent (Appointment.ConfirmationSent = false). That flag works as the "pending email"
/// record: if Brevo was down or the process restarted between the booking and the sending, the
/// confirmation still goes out on the next sweep. Only bookings from the last 24 hours, for future
/// dates, are retried, and each one at most <see cref="MaxAttempts"/> times per process.
/// The cancellation notice is not persisted (the booking data is cleared on cancel): it is sent once.
/// </summary>
public sealed class EmailDispatcherService : BackgroundService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(2);

    private readonly EmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailDispatcherService> _logger;
    private readonly ConcurrentDictionary<int, int> _attempts = new();

    public EmailDispatcherService(EmailQueue queue, IServiceScopeFactory scopeFactory, ILogger<EmailDispatcherService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextSweep = DateTime.UtcNow.AddSeconds(30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var wait = nextSweep - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeout.CancelAfter(wait);
                    try
                    {
                        if (await _queue.Reader.WaitToReadAsync(timeout.Token))
                        {
                            while (_queue.Reader.TryRead(out var job))
                            {
                                await ProcessAsync(job, stoppingToken);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Time for the sweep.
                    }
                }

                if (DateTime.UtcNow >= nextSweep)
                {
                    await RetryPendingConfirmationsAsync(stoppingToken);
                    nextSweep = DateTime.UtcNow.Add(SweepInterval);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the email dispatcher");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ContinueWith(_ => { });
            }
        }
    }

    private async Task ProcessAsync(EmailJob job, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        if (job.CancellationSnapshot is { } snapshot)
        {
            if (!await email.SendCancellationAsync(snapshot, ct))
            {
                _logger.LogWarning("The cancellation notice for appointment {AppointmentId} could not be sent.", snapshot.Id);
            }
            return;
        }

        if (job.ConfirmationAppointmentId is { } id)
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SendConfirmationAsync(db, email, id, ct);
        }
    }

    private async Task RetryPendingConfirmationsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var since = DateTime.UtcNow.AddHours(-24);
        var today = ArgentinaDate.Today();

        var pendingIds = await db.Appointments
            .Where(a => a.Status == AppointmentStatus.Booked
                        && !a.ConfirmationSent
                        && a.BookedAt >= since
                        && a.Date >= today)
            .Select(a => a.Id)
            .Take(50)
            .ToListAsync(ct);

        foreach (var id in pendingIds)
        {
            if (_attempts.GetValueOrDefault(id) >= MaxAttempts)
            {
                continue;
            }

            await SendConfirmationAsync(db, email, id, ct);
        }
    }

    private async Task SendConfirmationAsync(AppDbContext db, IEmailService email, int id, CancellationToken ct)
    {
        var appointment = await db.Appointments
            .AsNoTracking()
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        // Already sent (e.g. by the sweep) or cancelled in the meantime: nothing to do.
        if (appointment is null || appointment.Status != AppointmentStatus.Booked || appointment.ConfirmationSent)
        {
            return;
        }

        var attempt = _attempts.AddOrUpdate(id, 1, (_, n) => n + 1);
        var sent = await email.SendConfirmationAsync(appointment, ct);

        if (sent)
        {
            _attempts.TryRemove(id, out _);
            // Only marks it if it is still the same booking (it could have been cancelled and booked again).
            await db.Appointments
                .Where(a => a.Id == id && a.Status == AppointmentStatus.Booked && a.BookedAt == appointment.BookedAt)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.ConfirmationSent, true), ct);
        }
        else
        {
            _logger.LogWarning(
                "The confirmation email for appointment {AppointmentId} could not be sent (attempt {Attempt} of {Max}).",
                id, attempt, MaxAttempts);
        }
    }
}
