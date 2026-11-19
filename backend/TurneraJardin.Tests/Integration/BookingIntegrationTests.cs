using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TurneraJardin.Api.Controllers;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;
using Xunit;

namespace TurneraJardin.Tests.Integration;

/// <summary>
/// Booking and cancellation through the real controller and a real MySQL: the conditional UPDATE,
/// the transaction with the history row and the concurrency tokens only show their behaviour there.
/// </summary>
public class BookingIntegrationTests : IDisposable
{
    private readonly MySqlDatabase _database = new();
    private readonly FakeEmailQueue _emails = new();
    private readonly FakeOutputCache _cache = new();

    public BookingIntegrationTests()
    {
        if (string.IsNullOrWhiteSpace(MySqlDatabase.ServerConnectionString)) return;
        _database.Seed();
    }

    public void Dispose()
    {
        if (string.IsNullOrWhiteSpace(MySqlDatabase.ServerConnectionString)) return;
        _database.Dispose();
    }

    [MySqlFact]
    public async Task TenFamiliesAtTheSameTime_OnlyOneGetsTheSlot()
    {
        var id = AddAppointment(TeacherCycle.SecondCycle);

        var gate = new UpdateGate(participants: 10);
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async i =>
        {
            await using var db = _database.CreateContext(gate);
            var result = await Controller(db).Book(id, Booking($"223555{i:D4}", $"familia{i}@example.com"), CancellationToken.None);
            return result.StatusOf();
        }));

        Assert.Equal(10, gate.Arrived); // all ten really raced to the UPDATE
        Assert.Equal(1, results.Count(s => s == 200));
        Assert.Equal(9, results.Count(s => s == 409));

        await using var check = _database.CreateContext();
        Assert.Equal(AppointmentStatus.Booked, check.Appointments.Single(a => a.Id == id).Status);
        Assert.Single(check.AppointmentEvents.Where(e => e.AppointmentId == id && e.Type == AppointmentEventType.Booked));
        Assert.Single(_emails.Confirmations);
        Assert.Contains(CachePolicies.AvailabilityTag, _cache.EvictedTags);
    }

    [MySqlFact]
    public async Task SameFamilyRetrying_GetsTheSameBooking_AndAnotherFamilyGets409()
    {
        var id = AddAppointment(TeacherCycle.SecondCycle);

        await using var db = _database.CreateContext();
        var first = await Controller(db).Book(id, Booking("0223 15 555-1234", "Ana@Example.com"), CancellationToken.None);
        var retry = await Controller(db).Book(id, Booking("+54 9 223 555 1234", "ana@example.com"), CancellationToken.None);
        var other = await Controller(db).Book(id, Booking("223 444 1234", "otra@example.com"), CancellationToken.None);

        Assert.Equal(200, first.StatusOf());
        Assert.Equal(200, retry.StatusOf());
        Assert.Equal(409, other.StatusOf());
        Assert.Equal(ErrorCodes.AppointmentTaken, other.Result.CodeOf());

        var stored = db.Appointments.AsNoTracking().Single(a => a.Id == id);
        Assert.Equal("5492235551234", stored.GuardianPhone); // stored normalized
        Assert.Single(_emails.Confirmations);
    }

    [MySqlFact]
    public async Task FirstCycleTeacher_AcceptsOnlineFollowUpInterviews()
    {
        // The cycle no longer forces in person: a follow-up interview (not the first) is online.
        var id = AddAppointment(TeacherCycle.FirstCycle);

        await using var db = _database.CreateContext();
        var result = await Controller(db).Book(id, Booking("2235551234", "a@example.com", InterviewMode.Online), CancellationToken.None);

        Assert.Equal(200, result.StatusOf());
        var stored = db.Appointments.AsNoTracking().Single(a => a.Id == id);
        Assert.Equal(AppointmentStatus.Booked, stored.Status);
        Assert.Equal(InterviewMode.Online, stored.Mode);
    }

    [MySqlFact]
    public async Task TodaysSlot_CannotBeBooked()
    {
        var id = AddAppointment(TeacherCycle.SecondCycle, ArgentinaDate.Today());

        await using var db = _database.CreateContext();
        var result = await Controller(db).Book(id, Booking("2235551234", "a@example.com"), CancellationToken.None);

        Assert.Equal(400, result.StatusOf());
        Assert.Equal(ErrorCodes.BookingTooSoon, result.Result.CodeOf());
    }

    [MySqlFact]
    public async Task InactiveTeacher_CannotBeBooked()
    {
        var id = AddAppointment(TeacherCycle.SecondCycle, active: false);

        await using var db = _database.CreateContext();
        var result = await Controller(db).Book(id, Booking("2235551234", "a@example.com"), CancellationToken.None);

        Assert.Equal(409, result.StatusOf());
    }

    [MySqlFact]
    public async Task Cancel_FreesTheSlot_AndKeepsTheHistory()
    {
        var id = AddAppointment(TeacherCycle.SecondCycle);
        await using (var db = _database.CreateContext())
        {
            Assert.Equal(200, (await Controller(db).Book(id, Booking("2235551234", "a@example.com"), CancellationToken.None)).StatusOf());
        }

        await using (var db = _database.CreateContext())
        {
            var wrongPhone = await Controller(db).Cancel(id, new CancelAppointmentDto { Phone = "2234441234" }, null, CancellationToken.None);
            Assert.Equal(404, wrongPhone.StatusOf());
            Assert.Equal(ErrorCodes.BookingNotFound, wrongPhone.CodeOf());
        }

        await using (var db = _database.CreateContext())
        {
            var ok = await Controller(db).Cancel(id, new CancelAppointmentDto { Phone = "+54 9 223 555-1234" }, null, CancellationToken.None);
            Assert.Equal(204, ok.StatusOf());
        }

        await using var check = _database.CreateContext();
        var appointment = check.Appointments.Single(a => a.Id == id);
        Assert.Equal(AppointmentStatus.Available, appointment.Status);
        Assert.Null(appointment.GuardianPhone);

        var events = check.AppointmentEvents.Where(e => e.AppointmentId == id).OrderBy(e => e.Id).Select(e => e.Type).ToList();
        Assert.Equal(new[] { AppointmentEventType.Booked, AppointmentEventType.CancelledByFamily }, events);
        // The snapshot keeps who had booked it, even though the slot is now empty.
        Assert.Equal("5492235551234", check.AppointmentEvents.Single(e => e.Type == AppointmentEventType.CancelledByFamily).GuardianPhone);
    }

    [MySqlFact]
    public async Task CancelledSlot_CanBeBookedAgainByAnotherFamily()
    {
        var id = AddAppointment(TeacherCycle.SecondCycle);
        await using (var db = _database.CreateContext())
        {
            await Controller(db).Book(id, Booking("2235551234", "a@example.com"), CancellationToken.None);
        }
        await using (var db = _database.CreateContext())
        {
            await Controller(db).Cancel(id, new CancelAppointmentDto { Phone = "2235551234" }, null, CancellationToken.None);
        }

        await using var again = _database.CreateContext();
        var result = await Controller(again).Book(id, Booking("2234441234", "b@example.com"), CancellationToken.None);

        Assert.Equal(200, result.StatusOf());
        Assert.Equal("b@example.com", again.Appointments.AsNoTracking().Single(a => a.Id == id).GuardianEmail);
    }

    private AppointmentsController Controller(AppDbContext db) =>
        new AppointmentsController(db, _emails, NullLogger<AppointmentsController>.Instance, _cache).WithHttpContext();

    private int AddAppointment(TeacherCycle cycle, DateOnly? date = null, bool active = true)
    {
        using var db = _database.CreateContext();
        var teacher = new Teacher
        {
            FirstName = "Docente",
            LastName = Guid.NewGuid().ToString("N")[..6],
            Email = $"{Guid.NewGuid():N}@example.com",
            Cycle = cycle,
            IsActive = active
        };
        var appointment = new Appointment
        {
            Teacher = teacher,
            Date = date ?? ArgentinaDate.FirstBookableDay().AddDays(2),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 20),
            Location = InterviewLocations.All[0]
        };
        db.Appointments.Add(appointment);
        db.SaveChanges();
        return appointment.Id;
    }

    private static BookAppointmentDto Booking(string phone, string email, InterviewMode mode = InterviewMode.InPerson) => new()
    {
        GuardianFirstName = "Laura",
        GuardianLastName = "Pérez",
        GuardianPhone = phone,
        GuardianEmail = email,
        ChildFirstName = "Tomás",
        ChildLastName = "Pérez",
        Mode = mode
    };
}
