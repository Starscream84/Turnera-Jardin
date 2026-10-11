namespace TurneraJardin.Api.Services;

/// <summary>
/// Interfaz para registrar eventos de auditoría en el sistema.
/// </summary>
public interface IAuditoriaService
{
    /// <summary>Registra una acción de auditoría.</summary>
    /// <param name="accion">Descripción de la acción realizada (ej: "LoginFallido", "CambioEstadoTurno")</param>
    /// <param name="tipo">Tipo de entidad afectada (ej: "Usuario", "Turno")</param>
    /// <param name="id">ID de la entidad (como string)</param>
    /// <param name="detalles">Detalles adicionales de la acción</param>
    Task RegistrarAsync(string accion, string tipo, string id, string? detalles = null);
}
