using Microsoft.EntityFrameworkCore;
using Pathy.Domain.Entities;

namespace Pathy.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the Pathy application.
/// </summary>
public class PathyDbContext : DbContext
{
    public PathyDbContext(DbContextOptions<PathyDbContext> options) : base(options)
    {
    }

    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PathyDbContext).Assembly);
    }
}
