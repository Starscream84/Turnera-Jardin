using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Data;

/// <summary>
/// Carga datos de ejemplo la primera vez que se crea la base (18 docentes, un usuario admin
/// y algunos turnos disponibles) para poder probar el sistema de punta a punta sin cargar
/// todo a mano. Ver README -> "Datos de prueba" para las credenciales generadas.
/// </summary>
public static class DbSeeder
{
    private static readonly (string Nombre, string Apellido, string Sala)[] DocentesDemo =
    {
        ("Ana",      "Gómez",     "Sala Celeste (Lactantes)"),
        ("Bruno",    "Rodríguez", "Sala Celeste (Lactantes)"),
        ("Carla",    "Fernández", "Sala Verde (1 año)"),
        ("Diego",    "López",     "Sala Verde (1 año)"),
        ("Elena",    "Martínez",  "Sala Verde (1 año)"),
        ("Franco",   "García",    "Sala Amarilla (2 años)"),
        ("Gisela",   "Pérez",     "Sala Amarilla (2 años)"),
        ("Hernán",   "Sánchez",   "Sala Amarilla (2 años)"),
        ("Ivana",    "Romero",    "Sala Naranja (3 años)"),
        ("Julián",   "Torres",    "Sala Naranja (3 años)"),
        ("Karina",   "Flores",    "Sala Naranja (3 años)"),
        ("Lucas",    "Acosta",    "Sala Roja (4 años)"),
        ("Marina",   "Benítez",   "Sala Roja (4 años)"),
        ("Nicolás",  "Suárez",    "Sala Roja (4 años)"),
        ("Ornella",  "Medina",    "Sala Azul (5 años)"),
        ("Pablo",    "Castro",    "Sala Azul (5 años)"),
        ("Quimey",   "Ríos",      "Educación Física"),
        ("Rocío",    "Molina",    "Música"),
    };

    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Docentes.Any())
        {
            return; // ya se sembraron datos antes
        }

        var docentes = DocentesDemo.Select(d => new Docente
        {
            Nombre = d.Nombre,
            Apellido = d.Apellido,
            Email = $"{d.Nombre.ToLowerInvariant()}.{d.Apellido.ToLowerInvariant()}@jardin.edu.ar"
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u"),
            Sala = d.Sala,
            Activo = true
        }).ToList();

        db.Docentes.AddRange(docentes);
        db.SaveChanges(); // se guarda ya para que EF asigne el Id de cada docente antes de generarles turnos

        var admin = new Usuario
        {
            Email = "admin@jardin.edu.ar",
            NombreCompleto = "Dirección del Jardín",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CambiarEsta123!"),
            Rol = RolUsuario.Admin,
            Activo = true
        };
        db.Usuarios.Add(admin);

        // Turnos de ejemplo: de lunes a viernes, 8:00 a 12:00, en bloques de 20 minutos,
        // para las próximas dos semanas, así se puede probar la reserva sin cargar nada a mano.
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var duracion = TimeSpan.FromMinutes(20);
        var horaInicioJornada = new TimeOnly(8, 0);
        var horaFinJornada = new TimeOnly(12, 0);

        foreach (var docente in docentes)
        {
            for (var fecha = hoy; fecha <= hoy.AddDays(14); fecha = fecha.AddDays(1))
            {
                if (fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                for (var hora = horaInicioJornada; hora.Add(duracion) <= horaFinJornada; hora = hora.Add(duracion))
                {
                    db.Turnos.Add(new Turno
                    {
                        DocenteId = docente.Id,
                        Fecha = fecha,
                        HoraInicio = hora,
                        HoraFin = hora.Add(duracion),
                        Estado = EstadoTurno.Disponible
                    });
                }
            }
        }

        db.SaveChanges();
    }
}
