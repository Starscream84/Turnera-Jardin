using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasIndex(t => t.Email).IsUnique();
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasOne(a => a.Teacher)
                  .WithMany(t => t.Appointments)
                  .HasForeignKey(a => a.TeacherId)
                  .OnDelete(DeleteBehavior.Cascade);

            // A teacher cannot have two appointments starting at the same time on the same day.
            entity.HasIndex(a => new { a.TeacherId, a.Date, a.StartTime }).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Teacher)
                  .WithMany()
                  .HasForeignKey(u => u.TeacherId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
