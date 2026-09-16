namespace TurneraJardin.Api.Models;

/// <summary>
/// Un docente del jardín, cada uno con su propia agenda de turnos.
/// </summary>
public class Docente
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public required string Apellido { get; set; }

    public required string Email { get; set; }

    /// <summary>Sala o grupo a cargo (ej: "Sala Celeste", "Sala de 2 años"). Informativo.</summary>
    public string? Sala { get; set; }

    /// <summary>Si está en false, no aparece en la agenda pública y no se le pueden generar turnos nuevos.</summary>
    public bool Activo { get; set; } = true;

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    public string NombreCompleto => $"{Nombre} {Apellido}";
    public ICollection<Disponibilidad> Disponibilidades { get; set; } = new List<Disponibilidad>();
}
