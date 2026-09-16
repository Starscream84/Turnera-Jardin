using System.ComponentModel.DataAnnotations;

namespace TurneraJardin.Api.Models;

/// <summary>
/// Plantilla semanal recurrente que define los rangos de atención de un docente.
/// </summary>
public class Disponibilidad
{
    public int Id { get; set; }

    public int DocenteId { get; set; }
    public Docente? Docente { get; set; }

    public DayOfWeek DiaSemana { get; set; }

    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    [Range(30, 180, ErrorMessage = "La duración mínima del bloque debe ser de 30 minutos.")]
    public int DuracionBloqueMinutos { get; set; } = 30;
}