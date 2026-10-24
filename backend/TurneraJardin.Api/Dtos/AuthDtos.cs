using System.ComponentModel.DataAnnotations;

namespace TurneraJardin.Api.Dtos;

public class LoginDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}

public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string FullName, string Role, int? TeacherId, string? PhotoUrl);
