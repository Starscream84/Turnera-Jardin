using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Dtos;

/// <summary>Docente visible públicamente al elegir con quién sacar el turno.</summary>
public record DocenteDto(int Id, string NombreCompleto, string? Sala, CicloDocente? Ciclo);

/// <summary>Docente con todos los campos, para el panel de administración.</summary>
public record DocenteAdminDto(int Id, string Nombre, string Apellido, string Email, string? Sala, bool Activo, CicloDocente? Ciclo);

public class DocenteCreateDto
{
    [Required, MaxLength(80)]
    public required string Nombre { get; set; }

    [Required, MaxLength(80)]
    public required string Apellido { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [MaxLength(80)]
    public string? Sala { get; set; }

    /// <summary>Ciclo. Null = sin ciclo (la familia elige presencial o virtual).</summary>
    public CicloDocente? Ciclo { get; set; }
}

public class DocenteUpdateDto
{
    [Required, MaxLength(80)]
    public required string Nombre { get; set; }

    [Required, MaxLength(80)]
    public required string Apellido { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [MaxLength(80)]
    public string? Sala { get; set; }

    /// <summary>Ciclo. Null = sin ciclo (la familia elige presencial o virtual).</summary>
    public CicloDocente? Ciclo { get; set; }

    public bool Activo { get; set; } = true;
}
