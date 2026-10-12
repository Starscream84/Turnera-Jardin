namespace TurneraJardin.Api.Services;

/// <summary>Datos inválidos del pedido. Se responde 400.</summary>
public class ValidacionException : Exception
{
    public IReadOnlyList<string> Errores { get; }

    public ValidacionException(string mensaje, IReadOnlyList<string>? errores = null) : base(mensaje)
    {
        Errores = errores ?? Array.Empty<string>();
    }
}

/// <summary>El pedido es válido pero choca con el estado actual (ej. turnos reservados afectados). Se responde 409.</summary>
public class ConflictoException : Exception
{
    public IReadOnlyList<string> Detalles { get; }

    public ConflictoException(string mensaje, IReadOnlyList<string>? detalles = null) : base(mensaje)
    {
        Detalles = detalles ?? Array.Empty<string>();
    }
}

/// <summary>El recurso pedido no existe. Se responde 404.</summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}
