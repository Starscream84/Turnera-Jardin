namespace TurneraJardin.Api.Models;

/// <summary>
/// Addresses where an in-person interview can take place. The coordinator picks one when
/// generating appointments, and it is stored as text in <see cref="Appointment.Location"/>,
/// so changing this list later does not alter appointments that were already generated.
/// To change an address or add another one, edit this list only.
/// </summary>
public static class InterviewLocations
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "Sede 1 — Jardin",
        "Sede 2 — Colegio",
        "Sede 3 — Superior",
    };

    public static bool IsValid(string? location) =>
        location is not null && All.Contains(location.Trim());
}
