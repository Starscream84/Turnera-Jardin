using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Auth;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>
/// Listado y gestión de turnos para el panel. Dirección y equipo técnico ven todo;
/// un docente solo ve y gestiona los turnos de su propia agenda.
/// </summary>
[ApiController]
[Route("api/admin/turnos")]
[Authorize(Policy = Politicas.Personal)]
public class AdminTurnosController : ControllerBase
{
    // Un turno Reservado solo puede completarse o cancelarse. Los demás estados son finales:
    // reabrir un turno cancelado podría pisar a otra familia que ya tomó ese horario.
    private static readonly EstadoTurno[] DestinosDesdeReservado = { EstadoTurno.Completado, EstadoTurno.Cancelado };

    private readonly AppDbContext _context;
    private readonly IAuditoriaService _auditoria;

    public AdminTurnosController(AppDbContext context, IAuditoriaService auditoria)
    {
        _context = context;
        _auditoria = auditoria;
    }

    /// <summary>Consulta de turnos con filtros opcionales. Un docente solo recibe los suyos.</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTurnos(
        [FromQuery] int? docenteId,
        [FromQuery] DateOnly? fecha,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] EstadoTurno? estado)
    {
        var query = _context.Turnos
            .AsNoTracking()
            .Include(t => t.Docente)
            .AsQueryable();

        if (User.EsDocente())
        {
            var propio = User.GetDocenteId();
            if (propio is null) return Forbid();

            // Se ignora el docenteId pedido: un docente no puede consultar la agenda de otro.
            query = query.Where(t => t.DocenteId == propio.Value);
        }
        else if (docenteId.HasValue && docenteId > 0)
        {
            query = query.Where(t => t.DocenteId == docenteId.Value);
        }

        if (fecha.HasValue) query = query.Where(t => t.Fecha == fecha.Value);
        if (desde.HasValue) query = query.Where(t => t.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(t => t.Fecha <= hasta.Value);
        if (estado.HasValue) query = query.Where(t => t.Estado == estado.Value);

        var turnos = await query
            .OrderBy(t => t.Fecha)
            .ThenBy(t => t.HoraInicio)
            .Select(t => new
            {
                t.Id,
                t.DocenteId,
                DocenteNombre = t.Docente != null ? t.Docente.Nombre + " " + t.Docente.Apellido : "Sin Asignar",
                t.Fecha,
                t.HoraInicio,
                t.HoraFin,
                t.NombrePadre,
                t.ApellidoPadre, t.TelefonoPadre,
                t.NombreNino,
                t.ApellidoNino, t.Observaciones,
                t.Estado,
                t.FechaReserva,
                t.ConfirmacionEnviada,
                t.RecordatorioEnviado
            })
            .ToListAsync();

        return Ok(turnos);
    }

    /// <summary>Cambia el estado de un turno reservado (Completado o Cancelado).</summary>
    [HttpPatch("{id:int}/estado")]
    public Task<IActionResult> CambiarEstado(int id, [FromBody] EstadoTurno nuevoEstado) =>
        AplicarCambioEstadoAsync(id, nuevoEstado);

    /// <summary>Cancela un turno reservado. Ruta que ya usa el frontend; equivale a PATCH estado=Cancelado.</summary>
    [HttpPost("{id:int}/cancelar")]
    public Task<IActionResult> Cancelar(int id) =>
        AplicarCambioEstadoAsync(id, EstadoTurno.Cancelado);

    private async Task<IActionResult> AplicarCambioEstadoAsync(int id, EstadoTurno nuevoEstado)
    {
        var turno = await _context.Turnos.FindAsync(id);

        // Para un docente, un turno ajeno se responde igual que uno inexistente.
        if (turno is null || !PuedeGestionar(turno))
        {
            return NotFound(new { mensaje = "El turno no existe." });
        }

        if (turno.Estado != EstadoTurno.Reservado || !DestinosDesdeReservado.Contains(nuevoEstado))
        {
            return Conflict(new { mensaje = $"No se puede pasar un turno de {turno.Estado} a {nuevoEstado}." });
        }

        var anterior = turno.Estado;
        turno.Estado = nuevoEstado;

        await _auditoria.RegistrarAsync("CambioEstadoTurno", "Turno", turno.Id.ToString(), $"{anterior} → {nuevoEstado}");

        return Ok(new { mensaje = "Estado actualizado correctamente.", turnoId = turno.Id, nuevoEstado = turno.Estado });
    }

    private bool PuedeGestionar(Turno turno)
    {
        if (!User.EsDocente()) return true;

        var propio = User.GetDocenteId();
        return propio.HasValue && propio.Value == turno.DocenteId;
    }
}
