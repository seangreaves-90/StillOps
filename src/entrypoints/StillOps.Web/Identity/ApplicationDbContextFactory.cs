using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StillOps.Web.Identity;

/// <summary>
/// Design-time factory used exclusively by EF Core tooling (dotnet ef migrations add/update).
/// At runtime, ApplicationDbContext is registered via AddIdentityInfrastructure() using the
/// Aspire-provisioned PostgreSQL connection string injected by WithReference(identityDb).
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=stillops-identity;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__efmigrationshistory", "identity"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
