namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Ciclo al que pertenece el docente. Define si la familia puede elegir entre presencial y virtual.
/// Un docente sin ciclo (null) se trata como segundo ciclo: la familia elige.
/// </summary>
public enum CicloDocente
{
    /// <summary>Lactantes y salas 1 y 2. La entrevista es siempre presencial.</summary>
    PrimerCiclo = 1,

    /// <summary>Salas 3, 4 y 5. La familia elige presencial o virtual.</summary>
    SegundoCiclo = 2,

    /// <summary>Docentes especialistas (música, educación física). La entrevista es siempre presencial.</summary>
    Especialista = 3
}
