using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Dtos;

/// <summary>Un usuario con acceso al panel (dirección o docente), tal como se muestra en el listado de administración.</summary>
public record UsuarioAdminDto(
    int Id,
    string Email,
    string NombreCompleto,
    RolUsuario Rol,
    int? DocenteId,
    string? DocenteNombre,
    bool Activo);

/// <summary>
/// Datos para crear un acceso nuevo. Si se manda <see cref="DocenteId"/>, se crea un login de tipo
/// Docente vinculado a ese docente (Email y NombreCompleto se toman del docente si no se especifican).
/// Si no se manda, se crea un usuario de tipo Admin y en ese caso Email y NombreCompleto son obligatorios.
/// </summary>
public class UsuarioCreateDto
{
    public int? DocenteId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? NombreCompleto { get; set; }

    /// <summary>Si no se manda, el sistema genera una contraseña temporal aleatoria.</summary>
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? Password { get; set; }

    public RolUsuario? Rol { get; set; }
}

/// <summary>Respuesta al crear un usuario o restablecer su contraseña: incluye la contraseña en texto plano una única vez.</summary>
public record UsuarioCredencialesDto(int Id, string Email, string NombreCompleto, string Rol, string PasswordTemporal);

public class RestablecerPasswordDto
{
    /// <summary>Si no se manda, el sistema genera una contraseña temporal aleatoria.</summary>
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? NuevaPassword { get; set; }
}

public class CambiarPasswordPropiaDto
{
    [Required]
    public required string PasswordActual { get; set; }

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public required string PasswordNueva { get; set; }
}
