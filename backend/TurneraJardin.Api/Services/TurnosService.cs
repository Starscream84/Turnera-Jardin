using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Utilities;

namespace TurneraJardin.Api.Services;

public class TurnosService : ITurnosService
{
    private readonly AppDbContext _context;

    public TurnosService(AppDbContext context)
    {
        _context = context;
    }

    // 1. Algoritmo de Cálculo Dinámico de Slots
    public async Task<List<TurnoSlotDto>> ObtenerSlotsDisponiblesAsync(int docenteId, DateOnly fecha)
    {
        var diaSemana = (int)fecha.DayOfWeek;

        // Obtener las reglas de disponibilidad configuradas para el docente ese día
        var disponibilidades = await _context.Disponibilidades
            .Where(d => d.DocenteId == docenteId && d.DiaSemana == diaSemana)
            .ToListAsync();

        if (!disponibilidades.Any())
        {
            return new List<TurnoSlotDto>();
        }

        // Consultar reservas activas en la fecha solicitada
        var turnosOcupados = await _context.Turnos
            .Where(t => t.DocenteId == docenteId
                    && t.Fecha == fecha
                    && t.Estado != EstadoTurno.Cancelado)
            .Select(t => t.HoraInicio)
            .ToListAsync();

        var slotsCalculados = new List<TurnoSlotDto>();

        foreach (var disp in disponibilidades)
        {
            var horaActual = disp.HoraInicio;
            var horaFin = disp.HoraFin;
            var intervalo = TimeSpan.FromMinutes(disp.DuracionBloqueMinutos);

            while (horaActual.Add(intervalo) <= horaFin)
            {
                var horaFinalSlot = horaActual.Add(intervalo);
                var estaReservado = turnosOcupados.Contains(horaActual);

                slotsCalculados.Add(new TurnoSlotDto
                {
                    DocenteId = docenteId,
                    Fecha = fecha,
                    HoraInicio = horaActual,
                    HoraFin = horaFinalSlot,
                    Disponible = !estaReservado
                });

                horaActual = horaFinalSlot;
            }
        }

        return slotsCalculados;
    }

    // 2. Gestión de Disponibilidad por la Dirección (Opción 3)
/* public async Task ActualizarDisponibilidadDocenteAsync(int docenteId, List<CrearDisponibilidadDto> nuevasReglas)
    {
        var asignacionesAnteriores = await _context.Disponibilidades
            .Where(d => d.DocenteId == docenteId)
            .ToListAsync();

        _context.Disponibilidades.RemoveRange(asignacionesAnteriores);

        foreach (var dto in nuevasReglas)
        {
            _context.Disponibilidades.Add(new Disponibilidad
            {
                DocenteId = docenteId,
                DiaSemana = dto.DiaSemana,
                HoraInicio = dto.HoraInicio,
                HoraFin = dto.HoraFin,
                DuracionBloqueMinutos = dto.DuracionBloqueMinutos
            });
        }

        await _context.SaveChangesAsync();
    }
*/

public async Task<Turno> ReservarTurnoAsync(ReservaTurnoDto dto)
{
    // 0. Validar datos de entrada
    var mensajeError = dto.ValidarDatos();
    if (mensajeError != null)
    {
        throw new ArgumentException(mensajeError);
    }

    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        var diaSemana = (int)dto.Fecha.DayOfWeek;

        // 1. Obtener de MySQL las disponibilidades del docente para ese día
        var disponibilidadesDocente = await _context.Disponibilidades
            .Where(d => d.DocenteId == dto.DocenteId && d.DiaSemana == diaSemana)
            .ToListAsync();

        // 2. Validar en memoria la franja horaria
        var duracionBloque = TimeSpan.FromMinutes(30); // Default, override if found in disponibilidad
        var disponibilidad = disponibilidadesDocente.FirstOrDefault(d =>
            d.HoraInicio <= dto.HoraInicio &&
            d.HoraFin >= dto.HoraInicio.Add(TimeSpan.FromMinutes(d.DuracionBloqueMinutos)));

        if (disponibilidad == null)
        {
            throw new InvalidOperationException("El docente no tiene disponibilidad configurada para ese horario.");
        }

        duracionBloque = TimeSpan.FromMinutes(disponibilidad.DuracionBloqueMinutos);

        // 3. Verificar que el slot no esté reservado por otro padre
        var turnoExistente = await _context.Turnos
            .FirstOrDefaultAsync(t => t.DocenteId == dto.DocenteId
                                && t.Fecha == dto.Fecha
                                && t.HoraInicio == dto.HoraInicio
                                && t.Estado != EstadoTurno.Cancelado);

        if (turnoExistente != null)
        {
            throw new InvalidOperationException("El turno seleccionado ya fue reservado por otra familia.");
        }

        // 4. Crear el turno
        var nuevoTurno = new Turno
        {
            DocenteId = dto.DocenteId,
            Fecha = dto.Fecha,
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraInicio.Add(duracionBloque),
            NombrePadre = dto.NombrePadre.Trim(),
            ApellidoPadre = dto.ApellidoPadre.Trim(),
            TelefonoPadre = dto.TelefonoPadre.Trim(),
            NombreNino = dto.NombreNino.Trim(),
            ApellidoNino = dto.ApellidoNino.Trim(),
            Observaciones = dto.Observaciones?.Trim(),
            Estado = EstadoTurno.Reservado,
            FechaReserva = DateTime.UtcNow,
            ConfirmacionEnviada = false
        };

        _context.Turnos.Add(nuevoTurno);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return nuevoTurno;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
}