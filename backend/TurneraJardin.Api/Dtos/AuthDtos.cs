using System.ComponentModel.DataAnnotations;

namespace TurneraJardin.Api.Dtos;

public class LoginDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}

/// <summary>Session returned by login and change-password. MustChangePassword: the panel must send the user to "Mi cuenta" first.</summary>
public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string FullName, string Role, int? TeacherId, string? PhotoUrl, bool MustChangePassword);
