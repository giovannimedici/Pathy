using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Pathy.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating PathyDbContext instances.
/// Used by EF Core tools for migrations when the application is not running.
/// </summary>
public class PathyDbContextFactory : IDesignTimeDbContextFactory<PathyDbContext>
{
    public PathyDbContext CreateDbContext(string[] args)
    {
        // Load configuration from environment variables or .env file
        var configuration = new ConfigurationBuilder()
            .SetBasePath(GetBasePath())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Set the ConnectionStrings__DefaultConnection environment variable.");

        var optionsBuilder = new DbContextOptionsBuilder<PathyDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(PathyDbContext).Assembly.FullName);
        });

        return new PathyDbContext(optionsBuilder.Options);
    }

    private static string GetBasePath()
    {
        // Navigate from Infrastructure project to API project to find appsettings
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiProjectPath = Path.Combine(currentDirectory, "..", "Pathy.API");
        
        if (Directory.Exists(apiProjectPath))
        {
            return Path.GetFullPath(apiProjectPath);
        }
        
        return currentDirectory;
    }
}
