namespace TurneraJardin.Api.Models;

/// <summary>
/// Registro de auditoría de acciones en el sistema.
/// </summary>
public class AuditoriaLog
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Detalles { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
