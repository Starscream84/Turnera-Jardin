using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using Pomelo.EntityFrameworkCore.MySql.Migrations.Internal;

#pragma warning disable EF1001 // MySqlHistoryRepository lives in Pomelo's "Internal" namespace; it is the documented extension point.

namespace TurneraJardin.Api.Data;

/// <summary>
/// Fix for Pomelo 9.0: while applying migrations it takes a MySQL lock named
/// "__{database name}_EFMigrationsLock", and MySQL rejects lock names longer than 64 characters.
/// Railway's database has a 48-character name, so the API crashed at startup (2026-11-12).
/// Here the lock name is short and fixed-length: "EFMigrationsLock_" + 16 hex characters derived
/// from the database name (still unique per database). Registered in Program.cs and in
/// DesignTimeDbContextFactory with ReplaceService.
/// </summary>
public class ShortLockMySqlHistoryRepository : MySqlHistoryRepository
{
    public ShortLockMySqlHistoryRepository(HistoryRepositoryDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override string GetDatabaseLockName(string databaseName) => LockNameFor(databaseName);

    public static string LockNameFor(string databaseName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(databaseName ?? ""));
        return "EFMigrationsLock_" + Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
