namespace TurneraJardin.Api.Models;

public class Disponibilidad
{
    public int Id { get; set; }
    public int DocenteId { get; set; }
    public virtual Docente? Docente { get; set; }
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    /// <summary>Duración de cada bloque de turno en minutos (ej: 15, 30, 60)</summary>
    public int DuracionBloqueMinutos { get; set; } = 30;
    public bool Activo { get; set; } = true;
}
