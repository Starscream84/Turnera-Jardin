namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Cycle the teacher belongs to. Defines whether the family can choose between in-person and online.
/// A teacher with no cycle (null) is treated as second cycle. The cycle no longer decides the
/// interview mode: the first interview is in person and the rest online, for every teacher.
/// </summary>
public enum TeacherCycle
{
    /// <summary>Infants and 1- and 2-year-old rooms.</summary>
    FirstCycle = 1,

    /// <summary>3-, 4- and 5-year-old rooms.</summary>
    SecondCycle = 2,

    /// <summary>Specialist teachers (music, physical education).</summary>
    Specialist = 3
}
