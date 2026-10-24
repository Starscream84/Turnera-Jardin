using Microsoft.EntityFrameworkCore;
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
    private static readonly (string Nombre, string Apellido, string Sala, CicloDocente? Ciclo, string Email)[] DocentesDemo =
    {
        ("Lucia", "Galera", "Deambuladores A", CicloDocente.PrimerCiclo, "luciasoledadgalera@gmail.com"),
        ("Ana Clara", "Osses", "Deambuladores B", CicloDocente.PrimerCiclo, "anaclaraosses21@gmail.com"),
        ("Belén", "Alias", "Maternal A y B", CicloDocente.PrimerCiclo, "belenalias4453@gmail.com"),
        ("Ana Belen", "Recchia", "Maternal D", CicloDocente.PrimerCiclo, "anabelenrecchia7@gmail.com"),
        ("Melanie", "Jaime", "Maternal C", CicloDocente.PrimerCiclo, "jaimemelanie13@gmail.com"),
        ("Ayelén", "Peralta", "Primera sección A y B", CicloDocente.SegundoCiclo, "ayelennperalta@gmail.com"),
        ("Belen", "Navarro", "Primera sección C", CicloDocente.SegundoCiclo, "belenavarrov@gmail.com"),
        ("Delfina", "Irigoyen", "Primera sección D", CicloDocente.SegundoCiclo, "irigoyendelfina9@gmail.com"),
        ("Paula", "Oviedo", "Segunda sección A", CicloDocente.SegundoCiclo, "paulaoviedo92@gmail.com"),
        ("Paola", "Cano", "Segunda sección B", CicloDocente.SegundoCiclo, "canopaola@gmail.com"),
        ("Ivana", "Salinas", "Segunda sección D", CicloDocente.SegundoCiclo, "ivi_giselle@hotmail.com"),
        ("Guadalupe", "Palavecino", "Segunda sección C", CicloDocente.SegundoCiclo, "laraguadalupepalavecino28@gmail.com"),
        ("Belen", "Perez Pegue", "Tercera sección A", CicloDocente.SegundoCiclo, "belenperezpegue@gmail.com"),
        ("Eugenia", "Crovetto", "Tercera sección B", CicloDocente.SegundoCiclo, "eugecrovetto@gmail.com"),
        ("Paula", "Sanna", "Tercera sección C", CicloDocente.SegundoCiclo, "paulasanna95@gmail.com"),
        ("Perla", "Liotine", "Tercera sección D", CicloDocente.SegundoCiclo, "liotineperla@gmail.com"),
    };

    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();
        // AsegurarColumnasTurnos(db); // Solo necesario en SQLite, no en MySQL

        if (db.Docentes.Any())
        {
            return; // ya se sembraron datos antes
        }

        var docentes = DocentesDemo.Select(d => new Docente
        {
            Nombre = d.Nombre,
            Apellido = d.Apellido,
            Email = d.Email,
            Sala = d.Sala,
            Ciclo = d.Ciclo,
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

    /// <summary>
    /// EnsureCreated() solo crea la base si no existe: no agrega columnas a una base ya creada.
    /// Como el proyecto no usa EF Migrations, las columnas nuevas de Turnos se agregan a mano acá
    /// cuando faltan, así no hay que borrar turnera.db (y perder los datos cargados) para tomar el cambio.
    /// </summary>
    private static void AsegurarColumnasTurnos(AppDbContext db)
    {
        // Nombre de columna -> tipo SQLite (EF Core guarda los enums como INTEGER y los DateTime como TEXT).
        var columnas = new (string Nombre, string Tipo)[]
        {
            ("Modalidad", "INTEGER NULL"),
            ("ConfirmacionSolicitada", "TEXT NULL"),
            ("EmailPadre", "TEXT NULL"),
        };

        db.Database.OpenConnection();
        try
        {
            using var comando = db.Database.GetDbConnection().CreateCommand();

            foreach (var (nombre, tipo) in columnas)
            {
                comando.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('Turnos') WHERE name = '{nombre}'";
                var existe = Convert.ToInt64(comando.ExecuteScalar()) > 0;

                if (!existe)
                {
                    comando.CommandText = $"ALTER TABLE Turnos ADD COLUMN {nombre} {tipo}";
                    comando.ExecuteNonQuery();
                }
            }
        }
        finally
        {
            db.Database.CloseConnection();
        }
    }
}
