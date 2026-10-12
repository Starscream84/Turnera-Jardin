using System.Globalization;

namespace TurneraJardin.Api.Services;

/// <summary>Regla semanal de atención: un docente atiende un día de la semana en un rango, partido en bloques fijos.</summary>
public readonly record struct ReglaHoraria(DayOfWeek DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin, int DuracionBloqueMinutos);

/// <summary>Intervalo horario ya tomado en una fecha (un turno reservado).</summary>
public readonly record struct OcupacionHoraria(TimeOnly HoraInicio, TimeOnly HoraFin);

public record SlotCalculado(TimeOnly HoraInicio, TimeOnly HoraFin, bool Disponible);

/// <summary>
/// Lógica pura de disponibilidad (sin base de datos), para poder probarla de forma aislada.
/// Todo el cálculo de horas se hace en minutos enteros: <see cref="TimeOnly.Add(TimeSpan)"/> da la
/// vuelta a medianoche en lugar de pasarse, y eso generaba bucles y slots falsos con rangos tardíos.
/// </summary>
public static class SlotCalculator
{
    // Mismos límites que el atributo [Range] del modelo Disponibilidad.
    public const int DuracionMinimaMinutos = 30;
    public const int DuracionMaximaMinutos = 180;

    private static readonly string[] Dias = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };

    public static string NombreDia(DayOfWeek dia) => Dias[(int)dia];

    public static string Formato(TimeOnly hora) => hora.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Dos intervalos se solapan si cada uno empieza antes de que termine el otro. Que uno termine justo cuando el otro empieza no cuenta.</summary>
    public static bool SeSolapan(TimeOnly aInicio, TimeOnly aFin, TimeOnly bInicio, TimeOnly bFin)
        => aInicio < bFin && bInicio < aFin;

    /// <summary>Devuelve la lista de problemas del conjunto de reglas; vacía si es válido.</summary>
    public static List<string> ValidarReglas(IReadOnlyList<ReglaHoraria> reglas)
    {
        var errores = new List<string>();
        var bienFormadas = new List<ReglaHoraria>();

        foreach (var r in reglas)
        {
            var etiqueta = $"{NombreDia(r.DiaSemana)} {Formato(r.HoraInicio)}–{Formato(r.HoraFin)}";

            if (r.HoraInicio.Second != 0 || r.HoraFin.Second != 0)
            {
                errores.Add($"{etiqueta}: las horas deben indicarse en horas y minutos, sin segundos.");
            }
            else if (r.HoraFin <= r.HoraInicio)
            {
                errores.Add($"{etiqueta}: la hora de fin debe ser posterior a la de inicio.");
            }
            else if (r.DuracionBloqueMinutos < DuracionMinimaMinutos || r.DuracionBloqueMinutos > DuracionMaximaMinutos)
            {
                errores.Add($"{etiqueta}: la duración del turno debe estar entre {DuracionMinimaMinutos} y {DuracionMaximaMinutos} minutos.");
            }
            else if ((Minutos(r.HoraFin) - Minutos(r.HoraInicio)) % r.DuracionBloqueMinutos != 0)
            {
                // Si no, el último tramo quedaría sin cubrir y nadie se daría cuenta.
                errores.Add($"{etiqueta}: el rango no se divide en turnos exactos de {r.DuracionBloqueMinutos} minutos.");
            }
            else
            {
                bienFormadas.Add(r);
            }
        }

        for (var i = 0; i < bienFormadas.Count; i++)
        {
            for (var j = i + 1; j < bienFormadas.Count; j++)
            {
                var a = bienFormadas[i];
                var b = bienFormadas[j];
                if (a.DiaSemana == b.DiaSemana && SeSolapan(a.HoraInicio, a.HoraFin, b.HoraInicio, b.HoraFin))
                {
                    errores.Add($"Las franjas del {NombreDia(a.DiaSemana)} {Formato(a.HoraInicio)}–{Formato(a.HoraFin)} y {Formato(b.HoraInicio)}–{Formato(b.HoraFin)} se superponen.");
                }
            }
        }

        return errores;
    }

    /// <summary>
    /// Parte las reglas del día de <paramref name="fecha"/> en turnos y marca cuáles están libres.
    /// Un turno está ocupado si se solapa con cualquier ocupación, no solo si empieza a la misma hora.
    /// Si se pasa <paramref name="ahoraLocal"/>, se descartan los turnos que ya empezaron.
    /// </summary>
    public static List<SlotCalculado> GenerarSlots(
        DateOnly fecha,
        IEnumerable<ReglaHoraria> reglas,
        IReadOnlyCollection<OcupacionHoraria> ocupaciones,
        DateTime? ahoraLocal = null)
    {
        var slots = new List<SlotCalculado>();
        var minutoActual = -1;

        if (ahoraLocal is DateTime ahora)
        {
            var hoy = DateOnly.FromDateTime(ahora);
            if (fecha < hoy) return slots;
            if (fecha == hoy) minutoActual = ahora.Hour * 60 + ahora.Minute;
        }

        foreach (var regla in reglas.Where(r => r.DiaSemana == fecha.DayOfWeek).OrderBy(r => r.HoraInicio))
        {
            if (regla.DuracionBloqueMinutos <= 0) continue;

            var fin = Minutos(regla.HoraFin);
            for (var inicio = Minutos(regla.HoraInicio); inicio + regla.DuracionBloqueMinutos <= fin; inicio += regla.DuracionBloqueMinutos)
            {
                if (inicio <= minutoActual) continue;

                var slotInicio = Hora(inicio);
                var slotFin = Hora(inicio + regla.DuracionBloqueMinutos);
                var libre = !ocupaciones.Any(o => SeSolapan(slotInicio, slotFin, o.HoraInicio, o.HoraFin));
                slots.Add(new SlotCalculado(slotInicio, slotFin, libre));
            }
        }

        return slots;
    }

    private static int Minutos(TimeOnly hora) => hora.Hour * 60 + hora.Minute;

    private static TimeOnly Hora(int minutos) => new(minutos / 60, minutos % 60);
}
