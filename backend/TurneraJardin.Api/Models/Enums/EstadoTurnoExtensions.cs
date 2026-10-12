namespace TurneraJardin.Api.Models.Enums;

public static class EstadoTurnoExtensions
{
    /// <summary>
    /// Estados que bloquean el horario. "Disponible" (turnos pre-generados sin reservar) y "Cancelado" no lo bloquean.
    /// Si se agrega un estado nuevo que ocupa el horario (ej. Confirmado), se suma solo acá.
    /// </summary>
    public static readonly EstadoTurno[] EstadosQueOcupan = { EstadoTurno.Reservado, EstadoTurno.Completado };

    public static bool OcupaHorario(this EstadoTurno estado) => EstadosQueOcupan.Contains(estado);
}
