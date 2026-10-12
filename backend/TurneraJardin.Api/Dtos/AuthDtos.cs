using System.ComponentModel.DataAnnotations;

namespace TurneraJardin.Api.Dtos;

public record LoginDto(
    string Email,
    string Password
);

/// <summary>Respuesta de login. Incluye los campos que ya consume el frontend (expiraUtc, docenteId).</summary>
public record AuthResponseDto(
    string Token,
    DateTime ExpiraUtc,
    int UsuarioId,
    string NombreCompleto,
    string Email,
    string Rol,
    int? DocenteId,
    bool DebeCambiarPassword
);

public class CambiarPasswordDto
{
    [Required]
    public string PasswordActual { get; set; } = "";

    [Required]
    public string PasswordNueva { get; set; } = "";
}
