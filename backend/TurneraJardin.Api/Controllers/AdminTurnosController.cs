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
    private readonly AppDbContext _context;

    public AdminTurnosController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Consulta administrativa de todos los turnos con filtros opcionales.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTurnos(
        [FromQuery] int? docenteId,
        [FromQuery] DateOnly? fecha,
        [FromQuery] EstadoTurno? estado)
    {
        var query = _context.Turnos
            .Include(t => t.Docente)
            .AsQueryable();

        if (docenteId.HasValue && docenteId > 0)
        {
            query = query.Where(t => t.DocenteId == docenteId.Value);
        }

        if (fecha.HasValue)
        {
            query = query.Where(t => t.Fecha == fecha.Value);
        }

        if (estado.HasValue)
        {
            query = query.Where(t => t.Estado == estado.Value);
        }

        var turnos = await query
            .OrderBy(t => t.Fecha)
            .ThenBy(t => t.HoraInicio)
            .Select(t => new
            {
                t.Id,
                t.DocenteId,
                DocenteNombre = t.Docente != null ? t.Docente.NombreCompleto : "Sin Asignar",
                t.Fecha,
                t.HoraInicio,
                t.HoraFin,
                t.NombrePadre,
                t.TelefonoPadre,
                t.NombreNino,
                t.Observaciones,
                t.Estado,
                t.FechaReserva
            })
            .ToListAsync();

        return Ok(turnos);
    }

    /// <summary>
    /// Cambia el estado de un turno reservado (ej. Atendido, Cancelado).
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] EstadoTurno nuevoEstado)
    {
        var turno = await _context.Turnos.FindAsync(id);

        if (turno == null)
        {
            return NotFound(new { mensaje = "El turno no existe." });
        }

        turno.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Estado actualizado correctamente.", turnoId = turno.Id, nuevoEstado = turno.Estado });
    }
}
