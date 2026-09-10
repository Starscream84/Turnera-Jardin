using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Dtos;

/// <summary>Franja disponible, tal como la ve un padre/madre antes de reservar.</summary>
public record TurnoDisponibleDto(int Id, DateOnly Fecha, TimeOnly HoraInicio, TimeOnly HoraFin);

/// <summary>Datos que completa el padre/madre para reservar un turno ya elegido.</summary>
public class ReservaTurnoDto
{
    [Required, MaxLength(120)]
    public required string NombrePadre { get; set; }

    /// <summary>Número de WhatsApp. Se acepta con o sin "+"; el backend lo normaliza.</summary>
    [Required, Phone, MaxLength(30)]
    public required string TelefonoPadre { get; set; }

    [Required, MaxLength(120)]
    public required string NombreNino { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>Confirmación devuelta al padre/madre luego de reservar con éxito.</summary>
public record TurnoConfirmadoDto(
    int Id,
    string DocenteNombre,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    string NombreNino
);

/// <summary>Vista completa de un turno para el panel de administración / docente.</summary>
public record TurnoAdminDto(
    int Id,
    int DocenteId,
    string DocenteNombre,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    EstadoTurno Estado,
    string? NombrePadre,
    string? TelefonoPadre,
    string? NombreNino,
    string? Observaciones,
    bool ConfirmacionEnviada,
    bool RecordatorioEnviado
);

/// <summary>Pedido para generar turnos disponibles en bloque para un docente.</summary>
public class GenerarTurnosDto
{
    [Required]
    public DateOnly FechaDesde { get; set; }

    [Required]
    public DateOnly FechaHasta { get; set; }

    /// <summary>Días de la semana en los que se abren turnos (0=Domingo..6=Sábado).</summary>
    [Required, MinLength(1)]
    public required List<DayOfWeek> DiasSemana { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Required]
    public TimeOnly HoraFin { get; set; }

    [Range(5, 240)]
    public int DuracionMinutos { get; set; } = 20;
}
