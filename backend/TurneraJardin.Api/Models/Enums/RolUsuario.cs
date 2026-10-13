namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Roles de acceso al panel de administración.
/// </summary>
public enum RolUsuario
{
    /// <summary>Acceso total: gestiona docentes, turnos, usuarios y configuración del sistema.</summary>
    SuperAdmin = 0,

    /// <summary>Acceso administrativo: gestiona docentes, turnos y usuarios.</summary>
    Admin = 1,

    /// <summary>Coordinador: gestiona turnos y docentes (sin acceso a usuarios).</summary>
    Coordinador = 2,

    /// <summary>Docente: solo ve y gestiona sus propios turnos.</summary>
    Docente = 3
}
