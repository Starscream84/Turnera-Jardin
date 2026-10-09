namespace TurneraJardin.Api.Services;

/// <summary>
/// Interfaz para registrar eventos de auditoría en el sistema.
/// </summary>
public interface IAuditoriaService
{
    /// <summary>Registra una acción de auditoría.</summary>
    Task RegistrarAsync(int? usuarioId, string accion, string? detalles = null);
}
