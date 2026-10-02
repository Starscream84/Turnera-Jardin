using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Interfaz para el servicio de gestión de turnos (citas/appointments) con docentes.
/// Maneja la disponibilidad, slots, y reservas de turnos.
/// </summary>
public interface ITurnosService
{
    /// <summary>
    /// Obtiene los slots disponibles para un docente en una fecha específica.
    /// Considera las reglas de disponibilidad configuradas y los turnos ya reservados.
    /// </summary>
    /// <param name="docenteId">ID del docente</param>
    /// <param name="fecha">Fecha para la que se desean obtener los slots</param>
    /// <returns>Lista de slots disponibles (algunos libres, otros ocupados)</returns>
    Task<List<TurnoSlotDto>> ObtenerSlotsDisponiblesAsync(int docenteId, DateOnly fecha);

    /// <summary>
    /// Actualiza las reglas de disponibilidad de un docente.
    /// Reemplaza completamente las reglas anteriores con las nuevas.
    /// </summary>
    /// <param name="docenteId">ID del docente</param>
    /// <param name="nuevasReglas">Nuevas reglas de disponibilidad (por día de semana y franja horaria)</param>
    /// <returns>Tarea asincrónica</returns>
    Task ActualizarDisponibilidadDocenteAsync(int docenteId, List<CrearDisponibilidadDto> nuevasReglas);

    /// <summary>
    /// Reserva un turno para un padre/madre con un docente.
    /// Valida disponibilidad, datos de contacto, y evita reservas duplicadas.
    /// </summary>
    /// <param name="dto">Datos de la reserva (docente, fecha, hora, datos del padre y niño)</param>
    /// <returns>El turno creado con confirmación</returns>
    /// <exception cref="ArgumentException">Si los datos de entrada son inválidos</exception>
    /// <exception cref="InvalidOperationException">Si el docente no está disponible o el slot fue reservado por otro</exception>
    Task<Turno> ReservarTurnoAsync(ReservaTurnoDto dto);
}
