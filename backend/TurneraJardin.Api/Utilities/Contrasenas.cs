using System.Text.RegularExpressions;

namespace TurneraJardin.Api.Utilities;

/// <summary>
/// Utilidades para validación y generación de contraseñas.
/// </summary>
public static class Contrasenas
{
    /// <summary>
    /// Valida que una contraseña cumpla con los requisitos mínimos de seguridad.
    /// Requisitos: Al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.
    /// </summary>
    public static bool EsValida(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < 8)
            return false;

        // Al menos una mayúscula
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;

        // Al menos una minúscula
        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;

        // Al menos un número
        if (!Regex.IsMatch(password, @"[0-9]"))
            return false;

        // Al menos un carácter especial
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_\-+=\[\]{};:'""\\|,.<>?/]"))
            return false;

        return true;
    }

    /// <summary>
    /// Genera una contraseña temporal aleatoria para ser entregada al usuario.
    /// </summary>
    public static string GenerarTemporal(int longitud = 12)
    {
        const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Range(0, longitud)
            .Select(_ => caracteres[random.Next(caracteres.Length)])
            .ToArray());
    }

    /// <summary>
    /// Obtiene un mensaje de error descriptivo si la contraseña no es válida.
    /// </summary>
    public static string? ObtenerMensajeError(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "La contraseña es requerida.";

        if (password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";

        if (!Regex.IsMatch(password, @"[A-Z]"))
            return "La contraseña debe contener al menos una mayúscula.";

        if (!Regex.IsMatch(password, @"[a-z]"))
            return "La contraseña debe contener al menos una minúscula.";

        if (!Regex.IsMatch(password, @"[0-9]"))
            return "La contraseña debe contener al menos un número.";

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_\-+=\[\]{};:'""\\|,.<>?/]"))
            return "La contraseña debe contener al menos un carácter especial (!@#$%^&* etc).";

        return null;
    }
}
