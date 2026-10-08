using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace TurneraJardin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Coordinador")]
public class AdminDocentesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITurnosService _turnosService;
    private readonly SlotCalculator _slotCalculator;
    private readonly ILogger<AdminDocentesController> _logger;

    public AdminDocentesController(
        AppDbContext context,
        ITurnosService turnosService,
        SlotCalculator slotCalculator,
        ILogger<AdminDocentesController> logger)
    {
        _context = context;
        _turnosService = turnosService;
        _slotCalculator = slotCalculator;
        _logger = logger;
    }

    // GET: api/admindocentes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Docente>>> Listar()
    {
        var docentes = await _context.Docentes
            .Where(d => d.Activo)
            .OrderBy(d => d.Apellido)
            .ToListAsync();

        return Ok(docentes);
    }

    // GET: api/admindocentes/{id}/turnos?mes=10&año=2026
    [HttpGet("{id}/turnos")]
    public async Task<ActionResult<IEnumerable<Turno>>> ObtenerTurnosDocente(
        int id,
        [FromQuery] int? mes = null,
        [FromQuery] int? año = null)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
            return NotFound();

        var query = _context.Turnos.Where(t => t.DocenteId == id);

        if (mes.HasValue && año.HasValue)
        {
            query = query.Where(t =>
                t.FechaTurno.Month == mes.Value &&
                t.FechaTurno.Year == año.Value);
        }

        var turnos = await query.OrderBy(t => t.FechaTurno).ToListAsync();
        return Ok(turnos);
    }

    // POST: api/admindocentes/{docenteId}/generar-turnos
    // Body: { "fechaDesde": "2026-10-15", "fechaHasta": "2026-12-15", 
    //         "diasSemana": [1,3,5], "duracionMinutos": 30 }
    [HttpPost("{docenteId}/generar-turnos")]
    public async Task<ActionResult> GenerarTurnos(int docenteId, [FromBody] GenerarTurnosRequest request)
    {
        try
        {
            var docente = await _context.Docentes.FindAsync(docenteId);
            if (docente == null)
                return NotFound("Docente no encontrado");

            if (request.FechaDesde >= request.FechaHasta)
                return BadRequest("Fecha desde debe ser menor a fecha hasta");

            // Usar SlotCalculator para generar slots
            var reglasHorarias = new List<ReglaHoraria>
            {
                new ReglaHoraria
                {
                    DiasSemana = request.DiasSemanaByte,
                    HoraInicio = request.HoraInicio ?? new TimeSpan(9, 0, 0),
                    HoraFin = request.HoraFin ?? new TimeSpan(17, 0, 0),
                    DuracionMinutos = request.DuracionMinutos
                }
            };

            var slotsGenerados = _slotCalculator.CalcularSlotsDisponibles(
                request.FechaDesde,
                request.FechaHasta,
                reglasHorarias);

            // Crear turnos a partir de slots calculados
            var turnos = slotsGenerados.Select(slot => new Turno
            {
                DocenteId = docenteId,
                FechaTurno = slot.Fecha,
                HoraDesde = slot.Hora,
                HoraHasta = slot.Hora.AddMinutes(request.DuracionMinutos),
                Estado = EstadoTurno.Disponible,
                Modalidad = ModalidadEntrevista.Presencial
            }).ToList();

            _context.Turnos.AddRange(turnos);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                $"Generados {turnos.Count} turnos para docente {docenteId}");

            return Ok(new { message = $"Generados {turnos.Count} turnos", turnos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando turnos");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // PUT: api/admindocentes/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarDocenteRequest request)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.Apellido))
            docente.Apellido = request.Apellido;

        if (!string.IsNullOrEmpty(request.Nombre))
            docente.Nombre = request.Nombre;

        if (!string.IsNullOrEmpty(request.Email))
            docente.Email = request.Email;

        _context.Docentes.Update(docente);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/admindocentes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
            return NotFound();

        docente.Activo = false;
        _context.Docentes.Update(docente);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class GenerarTurnosRequest
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public int[] DiasSemanA { get; set; }  // 1=Lunes, 7=Domingo
    public int DuracionMinutos { get; set; }
    public TimeSpan? HoraInicio { get; set; }
    public TimeSpan? HoraFin { get; set; }

    public byte DiasSemanaByte => (byte)(DiasSemanA?.Aggregate(0, (acc, d) => acc | (1 << (d - 1))) ?? 0);
}

public class ActualizarDocenteRequest
{
    public string? Apellido { get; set; }
    public string? Nombre { get; set; }
    public string? Email { get; set; }
}