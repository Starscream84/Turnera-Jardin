using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Data;

/// <summary>
/// Loads seed data the first time the database is created (the kindergarten's teachers, an admin user
/// and some available appointments) so the system can be tested end to end without loading
/// everything by hand. See README -> "Datos de prueba" for the generated credentials.
/// </summary>
public static class DbSeeder
{
    private static readonly (string FirstName, string LastName, string Classroom, TeacherCycle? Cycle, string Email)[] SeedTeachers =
    {
        ("Lucia", "Galera", "Deambuladores A", TeacherCycle.FirstCycle, "luciasoledadgalera@gmail.com"),
        ("Ana Clara", "Osses", "Deambuladores B", TeacherCycle.FirstCycle, "anaclaraosses21@gmail.com"),
        ("Belén", "Alias", "Maternal A y B", TeacherCycle.FirstCycle, "belenalias4453@gmail.com"),
        ("Ana Belen", "Recchia", "Maternal D", TeacherCycle.FirstCycle, "anabelenrecchia7@gmail.com"),
        ("Melanie", "Jaime", "Maternal C", TeacherCycle.FirstCycle, "jaimemelanie13@gmail.com"),
        ("Ayelén", "Peralta", "Primera sección A y B", TeacherCycle.SecondCycle, "ayelennperalta@gmail.com"),
        ("Belen", "Navarro", "Primera sección C", TeacherCycle.SecondCycle, "belenavarrov@gmail.com"),
        ("Delfina", "Irigoyen", "Primera sección D", TeacherCycle.SecondCycle, "irigoyendelfina9@gmail.com"),
        ("Paula", "Oviedo", "Segunda sección A", TeacherCycle.SecondCycle, "paulaoviedo92@gmail.com"),
        ("Paola", "Cano", "Segunda sección B", TeacherCycle.SecondCycle, "canopaola@gmail.com"),
        ("Ivana", "Salinas", "Segunda sección D", TeacherCycle.SecondCycle, "ivi_giselle@hotmail.com"),
        ("Guadalupe", "Palavecino", "Segunda sección C", TeacherCycle.SecondCycle, "laraguadalupepalavecino28@gmail.com"),
        ("Belen", "Perez Pegue", "Tercera sección A", TeacherCycle.SecondCycle, "belenperezpegue@gmail.com"),
        ("Eugenia", "Crovetto", "Tercera sección B", TeacherCycle.SecondCycle, "eugecrovetto@gmail.com"),
        ("Paula", "Sanna", "Tercera sección C", TeacherCycle.SecondCycle, "paulasanna95@gmail.com"),
        ("Perla", "Liotine", "Tercera sección D", TeacherCycle.SecondCycle, "liotineperla@gmail.com"),
    };

    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();
        // EnsureAppointmentColumns(db); // Only needed on SQLite, not on MySQL

        if (db.Teachers.Any())
        {
            return; // data was already seeded
        }

        var teachers = SeedTeachers.Select(t => new Teacher
        {
            FirstName = t.FirstName,
            LastName = t.LastName,
            Email = t.Email,
            Classroom = t.Classroom,
            Cycle = t.Cycle,
            IsActive = true
        }).ToList();

        db.Teachers.AddRange(teachers);
        db.SaveChanges(); // saved now so EF assigns each teacher's Id before generating their appointments

        var admin = new User
        {
            Email = "admin@jardin.edu.ar",
            FullName = "Dirección del Jardín",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CambiarEsta123!"),
            Role = UserRole.Admin,
            IsActive = true
        };
        db.Users.Add(admin);

        // Sample appointments: Monday to Friday, 8:00 to 12:00, in 20-minute blocks,
        // for the next two weeks, so booking can be tested without loading anything by hand.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var duration = TimeSpan.FromMinutes(20);
        var dayStart = new TimeOnly(8, 0);
        var dayEnd = new TimeOnly(12, 0);

        foreach (var teacher in teachers)
        {
            for (var date = today; date <= today.AddDays(14); date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                for (var time = dayStart; time.Add(duration) <= dayEnd; time = time.Add(duration))
                {
                    db.Appointments.Add(new Appointment
                    {
                        TeacherId = teacher.Id,
                        Date = date,
                        StartTime = time,
                        EndTime = time.Add(duration),
                        Status = AppointmentStatus.Available
                    });
                }
            }
        }

        db.SaveChanges();
    }

    /// <summary>
    /// EnsureCreated() only creates the database if it doesn't exist: it doesn't add columns to an existing one.
    /// Since the project doesn't use EF Migrations, new Appointments columns were added here by hand
    /// when missing, so turnera.db didn't have to be deleted (losing the data) to pick up the change.
    /// </summary>
    private static void EnsureAppointmentColumns(AppDbContext db)
    {
        // Column name -> SQLite type (EF Core stores enums as INTEGER and DateTime as TEXT).
        var columns = new (string Name, string Type)[]
        {
            ("Mode", "INTEGER NULL"),
            ("ConfirmationRequestedAt", "TEXT NULL"),
            ("GuardianEmail", "TEXT NULL"),
        };

        db.Database.OpenConnection();
        try
        {
            using var command = db.Database.GetDbConnection().CreateCommand();

            foreach (var (name, type) in columns)
            {
                command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('Appointments') WHERE name = '{name}'";
                var exists = Convert.ToInt64(command.ExecuteScalar()) > 0;

                if (!exists)
                {
                    command.CommandText = $"ALTER TABLE Appointments ADD COLUMN {name} {type}";
                    command.ExecuteNonQuery();
                }
            }
        }
        finally
        {
            db.Database.CloseConnection();
        }
    }
}
