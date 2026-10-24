namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// How the family wants to have the interview. Chosen by the parent when booking.
/// </summary>
public enum InterviewMode
{
    /// <summary>The family comes to the kindergarten.</summary>
    InPerson = 0,

    /// <summary>The interview is held by video call.</summary>
    Online = 1
}
