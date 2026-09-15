using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pathy.Infrastructure.Data;

namespace Pathy.Tests.Integration;

/// <summary>
/// Custom Web Application Factory for integration tests.
/// </summary>
public class PathyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configure test settings (JWT, etc.)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add in-memory configuration with test JWT settings
            var testConfiguration = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-jwt-secret-key-with-at-least-32-characters-for-security",
                ["Jwt:Issuer"] = "PathyTestIssuer",
                ["Jwt:Audience"] = "PathyTestAudience",
                ["Jwt:ExpirationInMinutes"] = "60"
            };

            config.AddInMemoryCollection(testConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real database registration
            services.RemoveAll(typeof(DbContextOptions<PathyDbContext>));
            services.RemoveAll(typeof(PathyDbContext));

            // Add in-memory database for testing
            services.AddDbContext<PathyDbContext>(options =>
            {
                options.UseInMemoryDatabase("PathyTestDb");
            });

            // Build the service provider and create/migrate the database
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PathyDbContext>();
            context.Database.EnsureCreated();
        });
    }
}
