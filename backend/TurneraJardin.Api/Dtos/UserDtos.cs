using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Dtos;

/// <summary>A user with access to the panel (management or teacher), as shown in the admin list.</summary>
public record UserAdminDto(
    int Id,
    string Email,
    string FullName,
    UserRole Role,
    int? TeacherId,
    string? TeacherName,
    bool IsActive,
    string? PhotoUrl);

/// <summary>
/// Data to create a new login. If <see cref="TeacherId"/> is sent, a Teacher login linked to that
/// teacher is created (Email and FullName are taken from the teacher if not specified).
/// Otherwise an Admin user is created, and in that case Email and FullName are required.
/// </summary>
public class UserCreateDto
{
    public int? TeacherId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? FullName { get; set; }

    /// <summary>If not sent, the system generates a random temporary password.</summary>
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? Password { get; set; }

    public UserRole? Role { get; set; }
}

/// <summary>Response when creating a user or resetting their password: includes the plain-text password only once.</summary>
public record UserCredentialsDto(int Id, string Email, string FullName, string Role, string TemporaryPassword);

public class ResetPasswordDto
{
    /// <summary>If not sent, the system generates a random temporary password.</summary>
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? NewPassword { get; set; }
}

public class ChangeOwnPasswordDto
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public required string NewPassword { get; set; }
}
