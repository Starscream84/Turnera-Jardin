using System.ComponentModel.DataAnnotations;

namespace TurneraJardin.Api.Dtos;

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    int UsuarioId,
    string NombreCompleto,
    string Email,
    string Rol
);