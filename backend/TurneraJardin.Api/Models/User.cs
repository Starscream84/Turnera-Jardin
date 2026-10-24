using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Models;

/// <summary>
/// User with access to the admin panel (principal, coordinator or teacher).
/// </summary>
public class User
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string FullName { get; set; }

    public UserRole Role { get; set; } = UserRole.Teacher;

    /// <summary>If Role = Teacher, reference to the matching Teacher record.</summary>
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Relative path (e.g. "/fotos-perfil/xxx.jpg") to the profile photo, served as a static file. Null if none.</summary>
    public string? PhotoUrl { get; set; }
}
