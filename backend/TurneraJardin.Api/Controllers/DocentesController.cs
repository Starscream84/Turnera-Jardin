using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;


namespace TurneraJardin.Api.Controllers;

/// <summary>Endpoints públicos: elegir docente y ver su disponibilidad. Sin autenticación.</summary>
[ApiController]
[Route("api/docentes")]
public class DocentesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITurnosService _turnosService;

    public DocentesController(AppDbContext db, ITurnosService turnosService)
    {
        _db = db;
        _turnosService = turnosService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DocenteDto>>> Listar()
    {
        var docentes = await _db.Docentes
            .Where(d => d.Activo)
            .OrderBy(d => d.Apellido).ThenBy(d => d.Nombre)
            .Select(d => new DocenteDto(d.Id, d.Nombre + " " + d.Apellido, d.Sala))
            .ToListAsync();

        return Ok(docentes);
    }

    /// <summary>Turnos libres de un docente en un rango de fechas (por defecto, los próximos 30 días).</summary>
    [HttpGet("{id:int}/turnos-disponibles")]
    public async Task<ActionResult<List<TurnoDisponibleDto>>> TurnosDisponibles(
        int id, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        var docenteExiste = await _db.Docentes.AnyAsync(d => d.Id == id && d.Activo);
        if (!docenteExiste)
        {
            return NotFound(new { mensaje = "Docente no encontrado." });
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var fechaDesde = desde is not null && desde >= hoy ? desde.Value : hoy;
        var fechaHasta = hasta ?? fechaDesde.AddDays(30);

        var turnos = await _db.Turnos
            .Where(t => t.DocenteId == id
                        && t.Estado == EstadoTurno.Disponible
                        && t.Fecha >= fechaDesde
                        && t.Fecha <= fechaHasta)
            .OrderBy(t => t.Fecha).ThenBy(t => t.HoraInicio)
            .Select(t => new TurnoDisponibleDto(t.Id, t.Fecha, t.HoraInicio, t.HoraFin))
            .ToListAsync();

        return Ok(turnos);
    }

    [HttpPut("{id}/disponibilidad")]
public async Task<IActionResult> ActualizarDisponibilidad(int id, [FromBody] List<CrearDisponibilidadDto> nuevasReglas)
{
    if (id <= 0 || nuevasReglas == null)
    {
        return BadRequest("Datos de solicitud inválidos.");
    }

    await _turnosService.ActualizarDisponibilidadDocenteAsync(id, nuevasReglas);
    return NoContent();
}
}
