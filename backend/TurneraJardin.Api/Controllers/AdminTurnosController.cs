using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Controllers;

/// <summary>
/// Listado y gestión de turnos para el panel de administración. Un Admin ve todo;
/// un usuario con rol Docente solo puede ver/gestionar los turnos de su propia agenda.
/// </summary>
[ApiController]
[Route("api/admin/turnos")]
[Authorize]
public class AdminTurnosController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminTurnosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TurnoAdminDto>>> Listar(
        [FromQuery] int? docenteId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] EstadoTurno? estado)
    {
        var query = _db.Turnos.Include(t => t.Docente).AsQueryable();

        if (EsDocente(out var docenteIdPropio))
        {
            // Un docente jamás puede ver la agenda de otro, sin importar qué pida por query string.
            query = query.Where(t => t.DocenteId == docenteIdPropio);
        }
        else if (docenteId is not null)
        {
            query = query.Where(t => t.DocenteId == docenteId);
        }

        if (desde is not null) query = query.Where(t => t.Fecha >= desde);
        if (hasta is not null) query = query.Where(t => t.Fecha <= hasta);
        if (estado is not null) query = query.Where(t => t.Estado == estado);

        var turnos = await query
            .OrderBy(t => t.Fecha).ThenBy(t => t.HoraInicio)
            .Select(t => new TurnoAdminDto(
                t.Id, t.DocenteId, t.Docente!.NombreCompleto, t.Fecha, t.HoraInicio, t.HoraFin,
                t.Estado, t.NombrePadre, t.TelefonoPadre, t.NombreNino, t.Observaciones,
                t.ConfirmacionEnviada, t.RecordatorioEnviado))
            .ToListAsync();

        return Ok(turnos);
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var turno = await _db.Turnos.FindAsync(id);
        if (turno is null)
        {
            return NotFound();
        }

        if (EsDocente(out var docenteIdPropio) && turno.DocenteId != docenteIdPropio)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        turno.Estado = EstadoTurno.Cancelado;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Elimina una franja "Disponible" que todavía nadie reservó (por ejemplo, para corregir un error de carga).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var turno = await _db.Turnos.FindAsync(id);
        if (turno is null)
        {
            return NotFound();
        }

        if (EsDocente(out var docenteIdPropio) && turno.DocenteId != docenteIdPropio)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (turno.Estado != EstadoTurno.Disponible)
        {
            return BadRequest(new { mensaje = "Solo se pueden eliminar turnos que todavía no fueron reservados. Si ya tiene una familia asignada, cancelalo en vez de eliminarlo." });
        }

        _db.Turnos.Remove(turno);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private bool EsDocente(out int docenteId)
    {
        docenteId = 0;
        if (!User.IsInRole(nameof(RolUsuario.Docente)))
        {
            return false;
        }

        var claim = User.FindFirstValue("docenteId");
        return claim is not null && int.TryParse(claim, out docenteId);
    }
}
