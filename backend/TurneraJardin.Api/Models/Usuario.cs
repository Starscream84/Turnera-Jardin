using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Models;

/// <summary>
/// Usuario con acceso al panel de administración (director/a o docente).
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string NombreCompleto { get; set; }

    public RolUsuario Rol { get; set; } = RolUsuario.Docente;

    /// <summary>Si Rol=Docente, referencia al registro de Docente correspondiente.</summary>
    public int? DocenteId { get; set; }
    public Docente? Docente { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>URL de la foto de perfil del usuario.</summary>
    public string? FotoUrl { get; set; }

    /// <summary>Indica si el usuario debe cambiar su contraseña en el próximo login.</summary>
    public bool DebeCambiarPassword { get; set; } = false;

    /// <summary>Hash de la contraseña temporal (generada por admin al resetear).</summary>
    public string? PasswordTemporalHash { get; set; }

    /// <summary>Fecha y hora (UTC) hasta la cual la contraseña temporal es válida.</summary>
    public DateTime? PasswordTemporalVenceUtc { get; set; }

    /// <summary>Versión de sesión para invalidar tokens activos (ej: después de cambiar contraseña).</summary>
    public int VersionSesion { get; set; } = 1;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
