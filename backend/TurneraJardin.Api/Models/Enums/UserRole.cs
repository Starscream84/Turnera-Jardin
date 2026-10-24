namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Access roles for the admin panel.
/// </summary>
public enum UserRole
{
    /// <summary>Full access: manages teachers, appointments and users.</summary>
    Admin = 0,

    /// <summary>Teacher: only sees and manages their own appointments.</summary>
    Teacher = 1,

    /// <summary>Coordinator: manages teachers and appointments, but not users.</summary>
    Coordinator = 2
}
