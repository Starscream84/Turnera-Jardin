using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Services;
using Xunit;

namespace TurneraJardin.Tests.Integration;

/// <summary>
/// Integration tests against a real MySQL (H28). They need the environment variable
/// TURNERA_TEST_MYSQL with a connection string WITHOUT database and with permission to create
/// and drop databases, e.g. "Server=127.0.0.1;Port=3306;User=root;Password=root".
/// Without it they are skipped, so "dotnet test" keeps working on a machine without MySQL.
/// In GitHub Actions the CI starts a MySQL 8.4 service and sets the variable.
/// </summary>
public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(MySqlDatabase.ServerConnectionString))
        {
            Skip = "TURNERA_TEST_MYSQL is not set (MySQL integration tests are skipped).";
        }
    }
}

/// <summary>
/// One throwaway database per test, created with the same options as Program.cs
/// (Pomelo, server 8.4, retries, short migration lock name) and dropped at the end.
/// </summary>
public sealed class MySqlDatabase : IDisposable
{
    public static string? ServerConnectionString => Environment.GetEnvironmentVariable("TURNERA_TEST_MYSQL");

    public string Name { get; }
    public string ConnectionString { get; }

    /// <param name="name">Database name. Default: a random one. Railway's is 48 characters long.</param>
    public MySqlDatabase(string? name = null)
    {
        Name = name ?? "tj_test_" + Guid.NewGuid().ToString("N")[..12];
        ConnectionString = new MySqlConnectionStringBuilder(ServerConnectionString!) { Database = Name }.ConnectionString;
    }

    public AppDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 4, 11)),
                mysql => mysql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null))
            .ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IHistoryRepository, ShortLockMySqlHistoryRepository>()
            .AddInterceptors(interceptors)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Runs DbSeeder exactly as the API does at startup (migrations + admin), in Production.</summary>
    public void Seed(string adminPassword = "ContraseñaDeAdmin1!")
    {
        using var db = CreateContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:AdminPassword"] = adminPassword })
            .Build();
        DbSeeder.Seed(db, config, new ProductionEnvironment(), NullLogger.Instance);
    }

    public void Execute(string sql)
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public long Scalar(string sql)
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    public void Dispose()
    {
        using var connection = new MySqlConnection(ServerConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{Name}`";
        command.ExecuteNonQuery();
    }

    private sealed class ProductionEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "TurneraJardin.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

/// <summary>
/// Holds every "UPDATE `Appointments`" until <paramref name="participants"/> requests have reached it
/// (or a timeout passes). Without this the requests of a test run almost one after the other and the
/// race between "read the slot" and "update it" never happens; with it, every family has already read
/// the slot as free when the first UPDATE runs, which is the worst case in production.
/// </summary>
public sealed class UpdateGate : DbCommandInterceptor
{
    private readonly int _participants;
    private readonly TaskCompletionSource _open = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrived;

    public UpdateGate(int participants) => _participants = participants;

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.TrimStart().StartsWith("UPDATE `Appointments`", StringComparison.OrdinalIgnoreCase))
        {
            if (Interlocked.Increment(ref _arrived) >= _participants) _open.TrySetResult();
            await Task.WhenAny(_open.Task, Task.Delay(TimeSpan.FromSeconds(10), cancellationToken));
        }
        return result;
    }

    public int Arrived => _arrived;
}

/// <summary>Output cache that only records which tags were evicted.</summary>
public sealed class FakeOutputCache : IOutputCacheStore
{
    public List<string> EvictedTags { get; } = new();

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        lock (EvictedTags) EvictedTags.Add(tag);
        return ValueTask.CompletedTask;
    }

    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken) => ValueTask.FromResult<byte[]?>(null);

    public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}

/// <summary>Email queue that only records what would have been sent.</summary>
public sealed class FakeEmailQueue : IEmailQueue
{
    public List<int> Confirmations { get; } = new();
    public List<Appointment> Cancellations { get; } = new();

    public void EnqueueConfirmation(int appointmentId)
    {
        lock (Confirmations) Confirmations.Add(appointmentId);
    }

    public void EnqueueCancellation(Appointment bookingSnapshot)
    {
        lock (Cancellations) Cancellations.Add(bookingSnapshot);
    }
}

public static class ControllerTestExtensions
{
    /// <summary>Gives the controller an HttpContext (ApiError reads TraceIdentifier from it).</summary>
    public static T WithHttpContext<T>(this T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    public static int? StatusOf<T>(this ActionResult<T> result) =>
        result.Result is Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult withStatus ? withStatus.StatusCode : null;

    public static int? StatusOf(this IActionResult result) =>
        result is Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult withStatus ? withStatus.StatusCode : null;

    /// <summary>The "code" of an ApiError answer (e.g. APPOINTMENT_TAKEN).</summary>
    public static string? CodeOf(this IActionResult? result) =>
        result is ObjectResult { Value: ProblemDetails problem } && problem.Extensions.TryGetValue("code", out var code)
            ? code?.ToString()
            : null;
}
