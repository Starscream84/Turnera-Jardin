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

    /// <summary>Appointments can be booked from tomorrow on (never for the same day).</summary>
    public static DateOnly FirstBookableDay() => Today().AddDays(1);

    /// <summary>
    /// How far ahead a family can book (≈ 6 months). The frontend uses the same number
    /// (BOOKING_DAYS_AHEAD in booking.service.ts) to know which dates to ask for.
    /// </summary>
    public const int BookingDaysAhead = 180;

    public static DateOnly LastBookableDay() => Today().AddDays(BookingDaysAhead);
}
