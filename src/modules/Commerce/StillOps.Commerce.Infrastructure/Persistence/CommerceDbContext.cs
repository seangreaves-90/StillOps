using Microsoft.EntityFrameworkCore;

namespace StillOps.Commerce.Infrastructure.Persistence;

public sealed class CommerceDbContext : DbContext
{
    public CommerceDbContext(DbContextOptions<CommerceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("commerce");
        base.OnModelCreating(modelBuilder);
    }
}
