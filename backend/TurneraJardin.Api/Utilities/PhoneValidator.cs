using System.Text.RegularExpressions;

namespace TurneraJardin.Api.Utilities;

/// <summary>
/// Validador de números telefónicos para Mar del Plata, Argentina.
/// Acepta números en formato E.164 sin "+" (ej: 54922355512345), listos para WhatsApp Cloud API.
/// </summary>
public static class PhoneValidator
{
    /// <summary>
    /// Código de área de Mar del Plata en formato E.164 sin "+": 549223
    /// Argentina: +54, Dígito 9 (móvil), Mar del Plata: 223
    /// </summary>
    private const string MarDelPlataPrefix = "549223";

    /// <summary>
    /// Patrón para validar número telefónico de Mar del Plata.
    /// Formato: 549223 (6 dígitos) + 8 dígitos locales = 14 dígitos totales
    /// Ejemplo válido: 54922355512345
    /// </summary>
    private static readonly Regex MarDelPlataPattern = new(
        $@"^{Regex.Escape(MarDelPlataPrefix)}\d{{8}}$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Valida que un número telefónico sea de Mar del Plata en formato E.164 sin "+".
    /// </summary>
    /// <param name="telefono">Número telefónico en formato E.164 sin "+" (ej: 54922355512345, 14 dígitos)</param>
    /// <returns>true si el número es válido para Mar del Plata; false en caso contrario</returns>
    public static bool EsValido(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
        {
            return false;
        }

        return MarDelPlataPattern.IsMatch(telefono);
    }

    /// <summary>
    /// Obtiene un mensaje de error descriptivo para un número inválido.
    /// </summary>
    /// <param name="telefono">Número telefónico a validar</param>
    /// <returns>Mensaje de error o string.Empty si es válido</returns>
    public static string GetMensajeError(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
        {
            return "El número de teléfono es obligatorio.";
        }

        if (telefono.Contains("+"))
        {
            return "El número debe estar en formato E.164 sin \"+\" (ej: 54922355512345).";
        }

        if (!telefono.All(char.IsDigit))
        {
            return "El número debe contener solo dígitos.";
        }

        if (!telefono.StartsWith(MarDelPlataPrefix))
        {
            return $"El número debe corresponder a Mar del Plata (comenzar con {MarDelPlataPrefix}).";
        }

        return $"El número de Mar del Plata debe tener 14 dígitos (formato: {MarDelPlataPrefix}XXXXXXXX).";
    }
}
