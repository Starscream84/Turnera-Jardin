using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Dtos;

namespace TurneraJardin.Api.Dtos;

/// <summary>Docente visible públicamente al elegir con quién sacar el turno.</summary>
public record DocenteDto(int Id, string NombreCompleto, string? Sala);

/// <summary>Docente con todos los campos, para el panel de administración.</summary>
public record DocenteAdminDto(int Id, string Nombre, string Apellido, string Email, string? Sala, bool Activo);

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

    public bool Activo { get; set; } = true;
}

