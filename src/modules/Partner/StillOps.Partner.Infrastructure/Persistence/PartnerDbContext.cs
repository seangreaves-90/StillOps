using Microsoft.EntityFrameworkCore;

namespace StillOps.Partner.Infrastructure.Persistence;

public sealed class PartnerDbContext : DbContext
{
    public PartnerDbContext(DbContextOptions<PartnerDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("partner");
        base.OnModelCreating(modelBuilder);
    }
}
