using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

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
        var diaSemana = fecha.DayOfWeek;

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
                var estaReservado = turnosOcupados.Contains(horaActual);

                slotsCalculados.Add(new TurnoSlotDto
                {
                    DocenteId = docenteId,
                    Fecha = fecha,
                    HoraInicio = horaActual,
                    HoraFin = horaActual.Add(intervalo),
                    Disponible = !estaReservado
                });

                horaActual = horaActual.Add(intervalo);
            }
        }

        return slotsCalculados;
    }

    // 2. Gestión de Disponibilidad por la Dirección (Opción 3)
    public async Task ActualizarDisponibilidadDocenteAsync(int docenteId, List<CrearDisponibilidadDto> nuevasReglas)
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
                HoraInicio = TimeOnly.FromTimeSpan(dto.HoraInicio),
                HoraFin = TimeOnly.FromTimeSpan(dto.HoraFin),
                DuracionBloqueMinutos = dto.DuracionBloqueMinutos
            });
        }

        await _context.SaveChangesAsync();
    }
}