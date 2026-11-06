using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Data;

/// <summary>
/// Creates the database the first time and, only if it is empty, loads the initial data:
///  - Teachers: from the "Seed:Teachers" configuration section (appsettings.Development.json, which is not
///    in the repo, or Seed__Teachers__0__FirstName=... environment variables). The repository holds no
///    real personal data. In Development, if nothing is configured, three fictional teachers are created.
///  - Admin user: admin@jardin.edu.ar with the password from "Seed:AdminPassword" (Seed__AdminPassword).
///    If it is not configured, a random one is generated and written ONCE to the log: change it on the
///    first login from "Mi cuenta".
///  - In Development only: sample available appointments for the next two weeks.
/// A database that already has teachers (production) is never touched.
/// </summary>
public static class DbSeeder
{
    private sealed class SeedTeacher
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Classroom { get; set; }
        public TeacherCycle? Cycle { get; set; }
    }

    private static readonly SeedTeacher[] DevelopmentTeachers =
    {
        new() { FirstName = "Docente", LastName = "de Prueba 1", Email = "docente1@example.com", Classroom = "Maternal A", Cycle = TeacherCycle.FirstCycle },
        new() { FirstName = "Docente", LastName = "de Prueba 2", Email = "docente2@example.com", Classroom = "Primera sección A", Cycle = TeacherCycle.SecondCycle },
        new() { FirstName = "Docente", LastName = "de Prueba 3", Email = "docente3@example.com", Classroom = "Música", Cycle = TeacherCycle.Specialist },
    };

    public static void Seed(AppDbContext db, IConfiguration config, IHostEnvironment env, ILogger logger)
    {
        db.Database.EnsureCreated();

        if (db.Teachers.Any())
        {
            return; // data was already seeded
        }

        var configured = config.GetSection("Seed:Teachers").Get<SeedTeacher[]>() ?? Array.Empty<SeedTeacher>();
        var seedTeachers = configured.Length > 0 ? configured : env.IsDevelopment() ? DevelopmentTeachers : Array.Empty<SeedTeacher>();

        var teachers = seedTeachers
            .Where(t => !string.IsNullOrWhiteSpace(t.FirstName) && !string.IsNullOrWhiteSpace(t.Email))
            .Select(t => new Teacher
            {
                FirstName = t.FirstName.Trim(),
                LastName = t.LastName.Trim(),
                Email = t.Email.Trim(),
                Classroom = t.Classroom?.Trim(),
                Cycle = t.Cycle,
                IsActive = true
            }).ToList();

        db.Teachers.AddRange(teachers);
        db.SaveChanges(); // saved now so EF assigns each teacher's Id before generating their appointments

        if (!db.Users.Any())
        {
            var password = config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12));
                logger.LogWarning(
                    "Initial admin user created: admin@jardin.edu.ar with the random password {Password}. " +
                    "Change it on the first login (Mi cuenta) or set Seed__AdminPassword before creating the database.",
                    password);
            }

            db.Users.Add(new User
            {
                Email = "admin@jardin.edu.ar",
                FullName = "Dirección del Jardín",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Admin,
                IsActive = true
            });
        }

        if (env.IsDevelopment())
        {
            AddSampleAppointments(db, teachers);
        }

        db.SaveChanges();
    }

    /// <summary>Monday to Friday, 8:00 to 12:00, in 20-minute blocks, for the next two weeks.</summary>
    private static void AddSampleAppointments(AppDbContext db, List<Teacher> teachers)
    {
        var today = ArgentinaDate.Today();
        var slots = Controllers.AppointmentSlots.ForDay(new TimeOnly(8, 0), new TimeOnly(12, 0), 20)!;

        foreach (var teacher in teachers)
        {
            for (var date = today; date <= today.AddDays(14); date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                foreach (var (start, end) in slots)
                {
                    db.Appointments.Add(new Appointment
                    {
                        TeacherId = teacher.Id,
                        Date = date,
                        StartTime = start,
                        EndTime = end,
                        Status = AppointmentStatus.Available,
                        Location = InterviewLocations.All[0]
                    });
                }
            }
        }
    }
}
