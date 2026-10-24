using System.ComponentModel.DataAnnotations;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Dtos;

/// <summary>Teacher shown publicly when choosing who to book the appointment with.</summary>
public record TeacherDto(int Id, string FullName, string? Classroom, TeacherCycle? Cycle);

/// <summary>Teacher with all fields, for the admin panel.</summary>
public record TeacherAdminDto(int Id, string FirstName, string LastName, string Email, string? Classroom, bool IsActive, TeacherCycle? Cycle);

public class TeacherCreateDto
{
    [Required, MaxLength(80)]
    public required string FirstName { get; set; }

    [Required, MaxLength(80)]
    public required string LastName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [MaxLength(80)]
    public string? Classroom { get; set; }

    /// <summary>Cycle. Null = no cycle (the family chooses in-person or online).</summary>
    public TeacherCycle? Cycle { get; set; }
}

public class TeacherUpdateDto
{
    [Required, MaxLength(80)]
    public required string FirstName { get; set; }

    [Required, MaxLength(80)]
    public required string LastName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [MaxLength(80)]
    public string? Classroom { get; set; }

    /// <summary>Cycle. Null = no cycle (the family chooses in-person or online).</summary>
    public TeacherCycle? Cycle { get; set; }

    public bool IsActive { get; set; } = true;
}
