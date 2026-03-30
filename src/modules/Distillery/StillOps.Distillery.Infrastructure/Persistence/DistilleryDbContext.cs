using Microsoft.EntityFrameworkCore;

namespace StillOps.Distillery.Infrastructure.Persistence;

public sealed class DistilleryDbContext : DbContext
{
    public DistilleryDbContext(DbContextOptions<DistilleryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("distillery");
        base.OnModelCreating(modelBuilder);
    }
}
