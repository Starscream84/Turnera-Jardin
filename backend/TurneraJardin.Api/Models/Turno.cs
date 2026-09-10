using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Models;

/// <summary>
/// Representa tanto una franja horaria disponible como, una vez reservada,
/// la entrevista concreta entre un docente y la familia. Se modela como una
/// sola entidad para simplificar: el admin/docente "abre" turnos (Estado=Disponible)
/// y un padre los reserva completando los datos de contacto.
/// </summary>
public class Turno
{
    public int Id { get; set; }

    public int DocenteId { get; set; }
    public Docente? Docente { get; set; }

    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    public EstadoTurno Estado { get; set; } = EstadoTurno.Disponible;

    // Datos completados al reservar
    public string? NombrePadre { get; set; }

    /// <summary>Teléfono en formato internacional E.164 sin "+" (ej: 5492235551234), listo para WhatsApp Cloud API.</summary>
    public string? TelefonoPadre { get; set; }

    public string? NombreNino { get; set; }
    public string? Observaciones { get; set; }

    public DateTime? FechaReserva { get; set; }

    /// <summary>Se pone en true apenas se envía (o se intenta enviar) el WhatsApp de confirmación.</summary>
    public bool ConfirmacionEnviada { get; set; } = false;

    /// <summary>Se pone en true cuando el job de recordatorios ya mandó el WhatsApp del día anterior.</summary>
    public bool RecordatorioEnviado { get; set; } = false;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
