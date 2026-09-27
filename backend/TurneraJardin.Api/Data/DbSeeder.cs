using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        Console.WriteLine(">>> EJECUTANDO DBSEEDER...");
        db.Database.EnsureCreated();

        // 1. CREAR O ACTUALIZAR USUARIO ADMIN
        var adminExistente = db.Usuarios.FirstOrDefault(u => u.Email == "admin@jardin.com");
        var nuevoHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

        if (adminExistente == null)
        {
            var admin = new Usuario
            {
                Email = "admin@jardin.com",
                PasswordHash = nuevoHash,
                NombreCompleto = "Directora",
                Rol = RolUsuario.Admin
            };

            db.Usuarios.Add(admin);
            Console.WriteLine(">>> USUARIO ADMIN CREADO EXITOSAMENTE EN MYSQL.");
        }
        else
        {
            adminExistente.PasswordHash = nuevoHash;
            Console.WriteLine(">>> CONTRASEÑA DE ADMIN RESTABLECIDA A 'Admin123!'.");
        }

        db.SaveChanges();

        // Si los docentes ya existen, evitamos volver a crearlos para no duplicar datos
        if (db.Docentes.Any())
        {
            Console.WriteLine(">>> DOCENTES YA EXISTENTES: El resto del seeder se omite.");
            return;
        }

        // 2. Crear Docentes
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
        db.SaveChanges();

        // 3. Crear Disponibilidades Semanales
        var disponibilidades = new List<Disponibilidad>();

        foreach (var docente in docentes)
        {
            for (int dia = 1; dia <= 5; dia++)
            {
                disponibilidades.Add(new Disponibilidad
                {
                    DocenteId = docente.Id,
                    DiaSemana = (DayOfWeek)dia,
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFin = new TimeOnly(12, 0),
                    DuracionBloqueMinutos = 30
                });

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
        Console.WriteLine(">>> DBSEEDER FINALIZADO CON ÉXITO.");
    }

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
}