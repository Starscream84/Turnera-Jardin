namespace TurneraJardin.Api.Services;

// Current date in Argentina time. The Railway server runs in UTC,
// so DateTime.Today would return the next day after 21:00.

public static class ArgentinaDate
{
    private static readonly TimeZoneInfo Zone = GetZone();

    private static TimeZoneInfo GetZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "ART", "ART");
        }
    }

    public static DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone));

    public static DateOnly FirstBookableDay() => Today().AddDays(1);
}
