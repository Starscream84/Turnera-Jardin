namespace TurneraJardin.Api.Services;

// Fecha actual segun la hora Argentina. El servidor Railway corre en UTC, 
// asi que DataTime.Today daria el dia siguiente despues de las 21:00.

public static class ArgentinaDate
{
    private static readonly TimeZoneInfo Zona = ObtenerZona();

    private static TimeZoneInfo ObtenerZona()
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

    public static DateOnly Hoy() =>
    DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona));

    public static DateOnly PrimerDiaReservable() => Hoy().AddDays(1);
}