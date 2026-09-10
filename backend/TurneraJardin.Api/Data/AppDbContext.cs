using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Docente>(entity =>
        {
            entity.HasIndex(d => d.Email).IsUnique();
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.HasOne(t => t.Docente)
                  .WithMany(d => d.Turnos)
                  .HasForeignKey(t => t.DocenteId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Un docente no puede tener dos turnos que arranquen a la misma hora el mismo día.
            entity.HasIndex(t => new { t.DocenteId, t.Fecha, t.HoraInicio }).IsUnique();
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Docente)
                  .WithMany()
                  .HasForeignKey(u => u.DocenteId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
