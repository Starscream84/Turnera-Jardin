using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Data;

/// <summary>
/// Carga datos de ejemplo en PostgreSQL: 18 docentes, usuario admin 
/// y sus reglas de disponibilidad semanal (Semana 2).
/// </summary>
public static class DbSeeder
{
    private static readonly (string Nombre, string Apellido, string Sala)[] DocentesDemo =
    {
        ("Ana",     "Gómez",     "Sala Celeste (Lactantes)"),
        ("Bruno",   "Rodríguez", "Sala Celeste (Lactantes)"),
        ("Carla",   "Fernández", "Sala Verde (1 año)"),
        ("Diego",   "López",     "Sala Verde (1 año)"),
        ("Elena",   "Martínez",  "Sala Verde (1 año)"),
        ("Franco",  "García",    "Sala Amarilla (2 años)"),
        ("Gisela",  "Pérez",     "Sala Amarilla (2 años)"),
        ("Hernán",  "Sánchez",   "Sala Amarilla (2 años)"),
        ("Ivana",   "Romero",    "Sala Naranja (3 años)"),
        ("Julián",  "Torres",    "Sala Naranja (3 años)"),
        ("Karina",  "Flores",    "Sala Naranja (3 años)"),
        ("Lucas",   "Acosta",    "Sala Roja (4 años)"),
        ("Marina",  "Benítez",   "Sala Roja (4 años)"),
        ("Nicolás", "Suárez",    "Sala Roja (4 años)"),
        ("Ornella", "Medina",    "Sala Azul (5 años)"),
        ("Pablo",   "Castro",    "Sala Azul (5 años)"),
        ("Quimey",  "Ríos",      "Educación Física"),
        ("Rocío",   "Molina",    "Música"),
    };

    public static void Seed(AppDbContext db)
    {
        Console.WriteLine(">>> EJECUTANDO DBSEEDER...");
        db.Database.EnsureCreated();

        if (db.Docentes.Any())
        {
            Console.WriteLine(">>> EL SEEDER SE SALTEÓ PORQUE YA HAY DOCENTES.");
            return;
        }

        // 1. Crear Docentes
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
        db.SaveChanges(); // Guardamos para generar los DocenteId

        // 2. Crear Usuario Admin
        var admin = new Usuario
        {
            Email = "admin@jardin.edu.ar",
            NombreCompleto = "Dirección del Jardín",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CambiarEsta123!"),
            Rol = RolUsuario.Admin,
            Activo = true
        };
        db.Usuarios.Add(admin);

        // 3. Crear Disponibilidades Semanales (Lunes a Viernes: 08:00-12:00 y 13:00-17:00 en bloques de 30 min)
        var disponibilidades = new List<Disponibilidad>();

        foreach (var docente in docentes)
        {
            for (int dia = 1; dia <= 5; dia++) // Lunes a Viernes
            {
                // Turno Mañana
                disponibilidades.Add(new Disponibilidad
                {
                    DocenteId = docente.Id,
                    DiaSemana = (DayOfWeek)dia,
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFin = new TimeOnly(12, 0),
                    DuracionBloqueMinutos = 30
                });

                // Turno Tarde
                disponibilidades.Add(new Disponibilidad
                {
                    DocenteId = docente.Id,
                    DiaSemana = (DayOfWeek)dia,
                    HoraInicio = new TimeOnly(13, 0),
                    HoraFin = new TimeOnly(17, 0),
                    DuracionBloqueMinutos = 30
                });
            }
        }

        db.Disponibilidades.AddRange(disponibilidades);
        db.SaveChanges();
        Console.WriteLine(">>> DBSEEDER FINALIZADO CON ÉXITO: Disponibilidades sembradas.");
    }
}