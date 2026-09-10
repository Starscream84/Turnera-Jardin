namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Roles de acceso al panel de administración.
/// </summary>
public enum RolUsuario
{
    /// <summary>Acceso total: gestiona docentes, turnos y usuarios.</summary>
    Admin = 0,

    /// <summary>Docente: solo ve y gestiona sus propios turnos.</summary>
    Docente = 1
}
