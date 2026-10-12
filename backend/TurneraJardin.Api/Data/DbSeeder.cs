using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        SembrarSuperAdmin(db);
    }

    private static void SembrarSuperAdmin(AppDbContext db)
    {
        if (db.Usuarios.Any(u => u.Rol == RolUsuario.SuperAdmin))
        {
            return;
        }

        db.Usuarios.Add(new Usuario
        {
            Email = "admin@turnera.edu.ar",
            NombreCompleto = "Equipo Técnico",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123!"),
            Rol = RolUsuario.SuperAdmin,
            DebeCambiarPassword = false
        });
        db.SaveChanges();
    }
}
