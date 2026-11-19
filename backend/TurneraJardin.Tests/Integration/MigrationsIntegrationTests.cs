using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using Xunit;

namespace TurneraJardin.Tests.Integration;

/// <summary>Startup of the API against a real MySQL: migrations, baseline of old databases and seed.</summary>
public class MigrationsIntegrationTests
{
    [MySqlFact]
    public void NewDatabase_IsCreatedByMigrations_WithTheInitialAdmin()
    {
        using var database = new MySqlDatabase();
        database.Seed();

        using var db = database.CreateContext();
        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.Equal(db.Database.GetMigrations().Count(), db.Database.GetAppliedMigrations().Count());

        var admin = db.Users.Single();
        Assert.Equal("admin@jardin.edu.ar", admin.Email);
        Assert.True(admin.MustChangePassword);
        Assert.True(BCrypt.Net.BCrypt.Verify("ContraseñaDeAdmin1!", admin.PasswordHash));
    }

    /// <summary>
    /// 2026-11-12 incident: Railway's database name is 48 characters long and Pomelo's lock name
    /// ("__{name}_EFMigrationsLock") went over MySQL's 64-character limit, so the API would not start.
    /// </summary>
    [MySqlFact]
    public void DatabaseWithRailwayLengthName_Migrates()
    {
        var name = "tj" + Guid.NewGuid().ToString("N") + "0123456789abcd"; // 48 characters, like Railway's
        Assert.Equal(48, name.Length);

        using var database = new MySqlDatabase(name);
        database.Seed();

        using var db = database.CreateContext();
        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.True(ShortLockMySqlHistoryRepository.LockNameFor(name).Length <= 64);
    }

    /// <summary>
    /// A database created with the old EnsureCreated() (tables, no __EFMigrationsHistory) is marked
    /// at the baseline and only the later migrations run, keeping its data.
    /// </summary>
    [MySqlFact]
    public void DatabaseCreatedBeforeMigrations_IsBaselinedAndKeepsItsData()
    {
        using var database = new MySqlDatabase();
        using (var db = database.CreateContext())
        {
            db.Database.Migrate(DbSeeder.BaselineMigrationId); // the schema EnsureCreated() used to create
        }

        database.Execute("DROP TABLE `__EFMigrationsHistory`");
        database.Execute(
            "INSERT INTO Teachers (FirstName, LastName, Email, Classroom, Cycle, IsActive) " +
            "VALUES ('Ana', 'Gómez', 'ana@example.com', 'Sala Azul', 2, 1)");

        database.Seed();

        using var after = database.CreateContext();
        Assert.Empty(after.Database.GetPendingMigrations());
        Assert.Equal("Ana", after.Teachers.Single().FirstName);
        Assert.Equal(0L, database.Scalar("SELECT COUNT(*) FROM AppointmentEvents")); // created by the later migration
    }

    [MySqlFact]
    public void Seed_TwiceInARow_ChangesNothing()
    {
        using var database = new MySqlDatabase();
        database.Seed();
        database.Seed("OtraContraseñaLarga2!");

        using var db = database.CreateContext();
        var admin = db.Users.Single();
        Assert.True(BCrypt.Net.BCrypt.Verify("ContraseñaDeAdmin1!", admin.PasswordHash));
    }
}
