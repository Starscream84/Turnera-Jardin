namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Forma en que la familia quiere tener la entrevista. La elige el padre/madre al reservar.
/// </summary>
public enum ModalidadEntrevista
{
    /// <summary>La familia va al jardín.</summary>
    Presencial = 0,

    /// <summary>La entrevista se hace por videollamada.</summary>
    Virtual = 1
}
