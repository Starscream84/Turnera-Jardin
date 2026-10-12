namespace TurneraJardin.Api.Dtos;

public class DocenteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Sala { get; set; }
    public bool Activo { get; set; }

    public DocenteDto() { }

    public DocenteDto(int id, string nombreCompleto, string? sala = null)
    {
        Id = id;
        var partes = nombreCompleto.Split(' ', 2);
        Nombre = partes[0];
        Apellido = partes.Length > 1 ? partes[1] : "";
        Sala = sala;
    }
}

public class DocenteAdminDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Sala { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int? UsuarioId { get; set; }

    public DocenteAdminDto() { }

    public DocenteAdminDto(int id, string nombre, string apellido, string email, string? telefono, string? sala, bool activo, DateTime fechaCreacion, int? usuarioId = null)
    {
        Id = id;
        Nombre = nombre;
        Apellido = apellido;
        NombreCompleto = $"{nombre} {apellido}";
        Email = email;
        Telefono = telefono;
        Sala = sala;
        Activo = activo;
        FechaCreacion = fechaCreacion;
        UsuarioId = usuarioId;
    }
}

public class DocenteCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Sala { get; set; }
}

public class DocenteUpdateDto
{
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Sala { get; set; }
    public bool? Activo { get; set; }
}

public class CrearDisponibilidadDto
{
    public int DocenteId { get; set; }
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int DuracionBloqueMinutos { get; set; } = 30;
}
