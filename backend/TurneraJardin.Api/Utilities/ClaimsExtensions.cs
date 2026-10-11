using System.Security.Claims;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Utilities;

/// <summary>
/// Métodos de extensión para ClaimsPrincipal, facilitando acceso a datos del JWT.
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>Obtiene el ID del usuario desde los claims del JWT.</summary>
    public static int GetUsuarioId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
        {
            throw new InvalidOperationException("Usuario ID no encontrado en los claims.");
        }
        return id;
    }

    /// <summary>Obtiene el ID del docente desde los claims del JWT.</summary>
    public static int? GetDocenteId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("DocenteId");
        if (claim == null || string.IsNullOrEmpty(claim.Value))
        {
            return null;
        }
        return int.TryParse(claim.Value, out var id) ? id : null;
    }

    /// <summary>Verifica si el usuario autenticado tiene rol de Docente.</summary>
    public static bool EsDocente(this ClaimsPrincipal user)
    {
        return user.IsInRole(nameof(RolUsuario.Docente));
    }

    /// <summary>Verifica si el usuario autenticado tiene rol de Admin o superior.</summary>
    public static bool EsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole(nameof(RolUsuario.Admin)) || user.IsInRole(nameof(RolUsuario.SuperAdmin));
    }

    /// <summary>Verifica si el usuario autenticado tiene rol de SuperAdmin.</summary>
    public static bool EsSuperAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole(nameof(RolUsuario.SuperAdmin));
    }
}
