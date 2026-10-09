using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Servicio para registrar eventos de auditoría en el sistema.
/// </summary>
public class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditoriaService> _logger;

    public AuditoriaService(AppDbContext context, ILogger<AuditoriaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Registra una acción de auditoría en la base de datos.</summary>
    public async Task RegistrarAsync(int? usuarioId, string accion, string? detalles = null)
    {
        try
        {
            var log = new AuditoriaLog
            {
                UsuarioId = usuarioId,
                Accion = accion,
                Detalles = detalles,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditoriaLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar auditoría: {Accion}", accion);
            // No lanzamos la excepción para que no interrumpa el flujo principal
        }
    }
}
