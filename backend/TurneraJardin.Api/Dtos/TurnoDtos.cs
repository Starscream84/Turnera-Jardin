using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Utilities;

namespace TurneraJardin.Api.Dtos;

/// <summary>Franja disponible, tal como la ve un padre/madre antes de reservar.</summary>
public record TurnoDisponibleDto(int Id, DateOnly Fecha, TimeOnly HoraInicio, TimeOnly HoraFin);

/// <summary>Datos que completa el padre/madre para reservar un turno ya elegido.</summary>
public record ReservaTurnoDto(
    int DocenteId,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    string NombrePadre,
    string ApellidoPadre,
    string TelefonoPadre,
    string NombreNino,
    string ApellidoNino,
    string? Observaciones
)
{
    /// <summary>Valida que los datos de la reserva sean correctos.</summary>
    /// <returns>Mensaje de error o null si es válido</returns>
    public string? ValidarDatos()
    {
        if (string.IsNullOrWhiteSpace(NombrePadre))
            return "El nombre del adulto es obligatorio.";

        if (string.IsNullOrWhiteSpace(ApellidoPadre))
            return "El apellido del adulto es obligatorio.";

        if (string.IsNullOrWhiteSpace(TelefonoPadre))
            return "El teléfono del adulto es obligatorio.";

        if (!PhoneValidator.EsValido(TelefonoPadre))
            return PhoneValidator.GetMensajeError(TelefonoPadre);

        if (string.IsNullOrWhiteSpace(NombreNino))
            return "El nombre del niño/a es obligatorio.";

        if (string.IsNullOrWhiteSpace(ApellidoNino))
            return "El apellido del niño/a es obligatorio.";

        return null; // Válido
    }
};

/// <summary>Confirmación devuelta al padre/madre luego de reservar con éxito.</summary>
public record TurnoConfirmadoDto(
    int Id,
    string DocenteNombre,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    string NombreNino,
    string ApellidoNino
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
    string? ApellidoPadre,
    string? TelefonoPadre,
    string? NombreNino,
    string? ApellidoNino,
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

public class TurnoSlotDto
{
    public int DocenteId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool Disponible { get; set; }
}
