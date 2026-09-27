using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface ITurnosService
{
    Task<List<TurnoSlotDto>> ObtenerSlotsDisponiblesAsync(int docenteId, DateOnly fecha);
    Task ActualizarDisponibilidadDocenteAsync(int docenteId, List<CrearDisponibilidadDto> nuevasReglas);
    Task<Turno> ReservarTurnoAsync(ReservaTurnoDto dto);
}
