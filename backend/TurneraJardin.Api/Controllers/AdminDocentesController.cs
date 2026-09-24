using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Controllers;

/// <summary>CRUD de docentes. Solo para administración (dirección del jardín).</summary>
[ApiController]
[Route("api/admin/docentes")]
[Authorize(Roles = $"{nameof(RolUsuario.Admin)},{nameof(RolUsuario.Coordinador)}")]
public class AdminDocentesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminDocentesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DocenteAdminDto>>> Listar()
    {
        var docentes = await _db.Docentes
            .OrderBy(d => d.Apellido).ThenBy(d => d.Nombre)
            .Select(d => new DocenteAdminDto(d.Id, d.Nombre, d.Apellido, d.Email, d.Sala, d.Activo))
            .ToListAsync();

        return Ok(docentes);
    }

    [HttpPost]
    public async Task<ActionResult<DocenteAdminDto>> Crear(DocenteCreateDto dto)
    {
        var yaExiste = await _db.Docentes.AnyAsync(d => d.Email.ToLower() == dto.Email.ToLower());
        if (yaExiste)
        {
            return Conflict(new { mensaje = "Ya existe un docente con ese email." });
        }

        var docente = new Docente
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = dto.Email.Trim(),
            Sala = dto.Sala?.Trim(),
            Activo = true
        };

        _db.Docentes.Add(docente);
        await _db.SaveChangesAsync();

        var resultado = new DocenteAdminDto(docente.Id, docente.Nombre, docente.Apellido, docente.Email, docente.Sala, docente.Activo);
        return CreatedAtAction(nameof(Listar), new { id = docente.Id }, resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, DocenteUpdateDto dto)
    {
        var docente = await _db.Docentes.FindAsync(id);
        if (docente is null)
        {
            return NotFound();
        }

        docente.Nombre = dto.Nombre.Trim();
        docente.Apellido = dto.Apellido.Trim();
        docente.Email = dto.Email.Trim();
        docente.Sala = dto.Sala?.Trim();
        docente.Activo = dto.Activo;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// No borra físicamente al docente (para no perder el historial de turnos ya realizados):
    /// lo desactiva, lo que lo saca de la agenda pública.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var docente = await _db.Docentes.FindAsync(id);
        if (docente is null)
        {
            return NotFound();
        }

        docente.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Genera en bloque turnos "Disponible" para un docente, según días de semana y horario.</summary>
    [HttpPost("{id:int}/generar-turnos")]
    public async Task<ActionResult<object>> GenerarTurnos(int id, GenerarTurnosDto dto)
    {
        var docente = await _db.Docentes.FindAsync(id);
        if (docente is null)
        {
            return NotFound(new { mensaje = "Docente no encontrado." });
        }

        if (dto.FechaHasta < dto.FechaDesde)
        {
            return BadRequest(new { mensaje = "La fecha 'hasta' no puede ser anterior a la fecha 'desde'." });
        }

        if (dto.HoraFin <= dto.HoraInicio)
        {
            return BadRequest(new { mensaje = "La hora de fin debe ser posterior a la hora de inicio." });
        }

        var existentes = await _db.Turnos
            .Where(t => t.DocenteId == id && t.Fecha >= dto.FechaDesde && t.Fecha <= dto.FechaHasta)
            .Select(t => new { t.Fecha, t.HoraInicio })
            .ToListAsync();
        var existentesSet = existentes.Select(e => (e.Fecha, e.HoraInicio)).ToHashSet();

        var nuevosTurnos = new List<Turno>();
        var duracion = TimeSpan.FromMinutes(dto.DuracionMinutos);

        for (var fecha = dto.FechaDesde; fecha <= dto.FechaHasta; fecha = fecha.AddDays(1))
        {
            if (!dto.DiasSemana.Contains(fecha.DayOfWeek))
            {
                continue;
            }

            for (var horaInicio = dto.HoraInicio; horaInicio.Add(duracion) <= dto.HoraFin; horaInicio = horaInicio.Add(duracion))
            {
                if (existentesSet.Contains((fecha, horaInicio)))
                {
                    continue; // ya hay un turno cargado en ese horario, no se duplica
                }

                nuevosTurnos.Add(new Turno
                {
                    DocenteId = id,
                    Fecha = fecha,
                    HoraInicio = horaInicio,
                    HoraFin = horaInicio.Add(duracion),
                    Estado = EstadoTurno.Disponible
                });
            }
        }

        _db.Turnos.AddRange(nuevosTurnos);
        await _db.SaveChangesAsync();

        return Ok(new { creados = nuevosTurnos.Count });
    }
}
