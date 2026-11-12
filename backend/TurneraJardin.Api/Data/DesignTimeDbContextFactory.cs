using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TurneraJardin.Api.Data;

/// <summary>
/// Used only by the "dotnet ef" tools (migrations add, script, has-pending-model-changes).
/// It creates the context without running Program.cs, so the tools work without a connection string,
/// a Jwt:Key or a database (e.g. in GitHub Actions). The server version must match the one in Program.cs.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=design_time;User=design;Password=design", new MySqlServerVersion(new Version(8, 4, 11)))
            .Options;
        return new AppDbContext(options);
    }
}
