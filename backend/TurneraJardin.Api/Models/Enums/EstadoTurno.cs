namespace TurneraJardin.Api.Models.Enums;

/// <summary>
/// Estados posibles de un turno a lo largo de su ciclo de vida.
/// </summary>
public enum EstadoTurno
{
    /// <summary>Franja horaria abierta por el docente, todavía sin reservar.</summary>
    Disponible = 0,

    /// <summary>Un padre/madre reservó el turno.</summary>
    Reservado = 1,

    /// <summary>El turno fue cancelado (por el padre o por administración).</summary>
    Cancelado = 2,

    /// <summary>La entrevista ya se realizó.</summary>
    Completado = 3
}
