using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>Endpoints públicos para que un padre/madre reserve o cancele su turno. Sin autenticación.</summary>
[ApiController]
[Route("api/turnos")]
public class TurnosController(AppDbContext db, IWhatsAppService whatsApp, ILogger<TurnosController> logger, ITurnosService turnosService) : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly IWhatsAppService _whatsApp = whatsApp;
    private readonly ILogger<TurnosController> _logger = logger;
    private readonly ITurnosService _turnosService = turnosService;

    [HttpPost("{id:int}/reservar")]
    public async Task<ActionResult<TurnoConfirmadoDto>> Reservar(int id, ReservaTurnoDto dto)
    {
        // Se usa una transacción para evitar que dos padres reserven el mismo turno al mismo tiempo.
        await using var transaccion = await _db.Database.BeginTransactionAsync();

        var turno = await _db.Turnos.Include(t => t.Docente).FirstOrDefaultAsync(t => t.Id == id);

        if (turno is null)
        {
            return NotFound(new { mensaje = "El turno no existe." });
        }

        if (turno.Estado != EstadoTurno.Disponible)
        {
            return Conflict(new { mensaje = "Este turno ya no está disponible. Elegí otro horario." });
        }

        turno.NombrePadre = dto.NombrePadre.Trim();
        turno.TelefonoPadre = dto.TelefonoPadre.Trim();
        turno.NombreNino = dto.NombreNino.Trim();
        turno.Observaciones = dto.Observaciones?.Trim();
        turno.Estado = EstadoTurno.Reservado;
        turno.FechaReserva = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        var enviado = await _whatsApp.EnviarConfirmacionAsync(turno);
        turno.ConfirmacionEnviada = enviado;
        await _db.SaveChangesAsync();

        if (!enviado)
        {
            _logger.LogWarning("El turno {TurnoId} se reservó pero no se pudo mandar el WhatsApp de confirmación.", turno.Id);
        }

        return Ok(new TurnoConfirmadoDto(
            turno.Id,
            turno.Docente!.NombreCompleto,
            turno.Fecha,
            turno.HoraInicio,
            turno.HoraFin,
            turno.NombreNino!));
    }

    /// <summary>
    /// Cancela un turno reservado. Por seguridad básica, se exige el mismo teléfono con el que se reservó.
    /// </summary>
    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromQuery] string telefono)
    {
        var turno = await _db.Turnos.FirstOrDefaultAsync(t => t.Id == id);

        if (turno is null || turno.Estado != EstadoTurno.Reservado)
        {
            return NotFound(new { mensaje = "No se encontró un turno reservado con ese id." });
        }

        var soloDigitosIngresado = new string(telefono.Where(char.IsDigit).ToArray());
        var soloDigitosGuardado = new string((turno.TelefonoPadre ?? "").Where(char.IsDigit).ToArray());

        if (soloDigitosIngresado != soloDigitosGuardado)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "El teléfono no coincide con el de la reserva." });
        }

        turno.Estado = EstadoTurno.Cancelado;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("slots")]
    public async Task<ActionResult<List<TurnoSlotDto>>> GetSlots([FromQuery] int docenteId, [FromQuery] DateOnly fecha)
    {
        if (docenteId <= 0)
        {
            return BadRequest("El ID del docente no es válido.");
        }

        var slots = await _turnosService.ObtenerSlotsDisponiblesAsync(docenteId, fecha);
        return Ok(slots);
    }
}
