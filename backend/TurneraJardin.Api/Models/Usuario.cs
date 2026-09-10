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
}
