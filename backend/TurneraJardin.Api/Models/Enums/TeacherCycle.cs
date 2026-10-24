namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Cycle the teacher belongs to. Defines whether the family can choose between in-person and online.
/// A teacher with no cycle (null) is treated as second cycle: the family chooses.
/// </summary>
public enum TeacherCycle
{
    /// <summary>Infants and 1- and 2-year-old rooms. The interview is always in person.</summary>
    FirstCycle = 1,

    /// <summary>3-, 4- and 5-year-old rooms. The family chooses in-person or online.</summary>
    SecondCycle = 2,

    /// <summary>Specialist teachers (music, physical education). The interview is always in person.</summary>
    Specialist = 3
}
