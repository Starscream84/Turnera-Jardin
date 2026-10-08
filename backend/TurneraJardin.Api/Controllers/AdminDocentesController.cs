cat > Controllers/AdminDocentesController.cs << 'EOF'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace TurneraJardin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Coordinador")]
public class AdminDocentesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminDocentesController> _logger;

    public AdminDocentesController(
        AppDbContext context,
        ILogger<AdminDocentesController> logger)
    {
        _context = context;
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
                t.Fecha.Month == mes.Value &&
                t.Fecha.Year == año.Value);
        }

        var turnos = await query.OrderBy(t => t.Fecha).ToListAsync();
        return Ok(turnos);
    }

    // POST: api/admindocentes/{docenteId}/generar-turnos
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

            var turnos = new List<Turno>();
            var fecha = DateOnly.FromDateTime(request.FechaDesde);
            var fechaHasta = DateOnly.FromDateTime(request.FechaHasta);
            var diasSemana = request.DiasSemanA ?? new int[] { };

            while (fecha <= fechaHasta)
            {
                int diaSemana = ((int)fecha.DayOfWeek + 6) % 7 + 1; // Convertir a 1=Lunes, 7=Domingo

                if (diasSemana >= 1 && diasSemana <= 7 && diasSemana <= diasSemana)
                {
                    var horaInicio = request.HoraInicio ?? new TimeOnly(9, 0);
                    var horaFin = request.HoraFin ?? new TimeOnly(17, 0);
                    var duracion = request.DuracionMinutos;

                    var hora = horaInicio;
                    while (hora.AddMinutes(duracion) <= horaFin)
                    {
                        var turno = new Turno
                        {
                            DocenteId = docenteId,
                            Fecha = fecha,
                            HoraInicio = hora,
                            HoraFin = hora.AddMinutes(duracion),
                            Estado = Models.Enums.EstadoTurno.Disponible,
                            Modalidad = "Presencial"
                        };
                        turnos.Add(turno);
                        hora = hora.AddMinutes(duracion);
                    }
                }

                fecha = fecha.AddDays(1);
            }

            _context.Turnos.AddRange(turnos);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Generados {turnos.Count} turnos para docente {docenteId}");

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
    public int[]? DiasSemanA { get; set; }
    public int DuracionMinutos { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
}

public class ActualizarDocenteRequest
{
    public string? Apellido { get; set; }
    public string? Nombre { get; set; }
    public string? Email { get; set; }
}
EOF