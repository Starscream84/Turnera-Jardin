using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Value that goes inside every JWT and is checked again on every request (Program.cs, OnTokenValidated).
/// It is derived from the password hash, so changing or resetting a password invalidates all tokens
/// issued before, with no new column in the database. Deactivating the user also cuts access right away,
/// because that same check verifies IsActive.
/// </summary>
public static class SecurityStamp
{
    public const string ClaimName = "stamp";

    public static string For(User user) => For(user.Id, user.PasswordHash);

    public static string For(int userId, string passwordHash)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{passwordHash}"));
        return Convert.ToBase64String(bytes, 0, 12);
    }
}

/// <summary>
/// Locks an email for a while after several failed logins, on top of the per-IP limit
/// (which alone does not stop someone rotating IPs). In memory: it resets if the API restarts.
/// </summary>
public sealed class LoginAttemptTracker
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockTime = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public LoginAttemptTracker(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string Key(string email) => $"login-failures:{email}";

    public bool IsLocked(string email) =>
        _cache.TryGetValue(Key(email), out int failures) && failures >= MaxFailures;

    public void RegisterFailure(string email)
    {
        var failures = _cache.TryGetValue(Key(email), out int current) ? current + 1 : 1;
        _cache.Set(Key(email), failures, LockTime);
    }

    public void Reset(string email) => _cache.Remove(Key(email));
}
