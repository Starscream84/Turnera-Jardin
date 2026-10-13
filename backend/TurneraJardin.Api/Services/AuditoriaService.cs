using System.Security.Claims;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Utilities;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Servicio para registrar eventos de auditoría en el sistema.
/// </summary>
public class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditoriaService> _logger;

    public AuditoriaService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditoriaService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>Registra una acción de auditoría en la base de datos.</summary>
    public async Task RegistrarAsync(string accion, string tipo, string id, string? detalles = null)
    {
        try
        {
            // Obtener el usuario autenticado desde el contexto HTTP
            var usuario = _httpContextAccessor.HttpContext?.User;
            int? usuarioId = null;

            if (usuario?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    usuarioId = usuario.GetUsuarioId();
                }
                catch
                {
                    // Si no se puede obtener el ID, simplemente no se asigna
                }
            }

            var log = new AuditoriaLog
            {
                UsuarioId = usuarioId,
                Accion = $"{tipo}:{accion}",
                Detalles = $"ID={id}" + (string.IsNullOrEmpty(detalles) ? "" : $" | {detalles}"),
                FechaUtc = DateTime.UtcNow
            };

            _context.AuditoriaLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar auditoría: {Accion}/{Tipo}", accion, tipo);
            // No lanzamos la excepción para que no interrumpa el flujo principal
        }
    }
}
