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
    public DbSet<Disponibilidad> Disponibilidades => Set<Disponibilidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Docente>(entity =>
        {
            entity.HasIndex(d => d.Email).IsUnique();
        });

// Configuración de Disponibilidad (Relación 1:N e Índice Compuesto)
        modelBuilder.Entity<Disponibilidad>(entity =>
        {
            entity.HasOne(d => d.Docente)
                .WithMany(doc => doc.Disponibilidades)
                .HasForeignKey(d => d.DocenteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Evita registrar dos veces la misma franja que inicie el mismo día a la misma hora
            entity.HasIndex(d => new { d.DocenteId, d.DiaSemana, d.HoraInicio }).IsUnique();
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
