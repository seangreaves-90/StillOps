using Microsoft.Extensions.Diagnostics.HealthChecks;
using StillOps.Web.Features.AdminPartner.Shell;

namespace StillOps.AppHost.IntegrationTests;

public sealed class PlatformHealthPresentationTests
{
    [Fact]
    public void BuildFreshItems_MapsCanonicalStarterTopology()
    {
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new(HealthStatus.Healthy, "ok", TimeSpan.Zero, null, null),
            ["stillops-ingestion"] = new(HealthStatus.Healthy, "ok", TimeSpan.Zero, null, null),
            ["stillops-identity-db"] = new(HealthStatus.Healthy, "ok", TimeSpan.Zero, null, null)
        }, TimeSpan.Zero);

        DateTimeOffset checkedAt = new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero);
        IReadOnlyList<PlatformHealthItemViewModel> items =
            PlatformHealthPresentation.BuildFreshItems(report, checkedAt);

        Assert.Collection(items,
            item =>
            {
                Assert.Equal("StillOps.Web", item.DisplayName);
                Assert.Equal("Healthy", item.Status);
                Assert.Equal(checkedAt, item.LastChecked);
            },
            item =>
            {
                Assert.Equal("StillOps.Ingestion", item.DisplayName);
                Assert.Equal("Healthy", item.Status);
                Assert.Equal(checkedAt, item.LastChecked);
            },
            item =>
            {
                Assert.Equal("PostgreSQL Identity Database", item.DisplayName);
                Assert.Equal("Healthy", item.Status);
                Assert.Equal(checkedAt, item.LastChecked);
            });
    }

    [Fact]
    public void BuildStaleItems_MarksAllRowsUnknownAndPreservesLastChecked()
    {
        DateTimeOffset checkedAt = new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero);
        IReadOnlyList<PlatformHealthItemViewModel> freshItems =
        [
            new("self", "StillOps.Web", "Healthy", "Trusted for routine operation.", checkedAt, null, null),
            new("stillops-ingestion", "StillOps.Ingestion", "Degraded", "Use caution and verify the affected workflow before proceeding.", checkedAt, "impact", "action"),
            new("stillops-identity-db", "PostgreSQL Identity Database", "Down", "Do not rely on this component for safe operation.", checkedAt, "impact", "action")
        ];

        IReadOnlyList<PlatformHealthItemViewModel> staleItems =
            PlatformHealthPresentation.BuildStaleItems(freshItems);

        Assert.All(staleItems, item =>
        {
            Assert.Equal("Unknown", item.Status);
            Assert.Equal(checkedAt, item.LastChecked);
            Assert.NotNull(item.Impact);
            Assert.NotNull(item.NextAction);
        });

        Assert.Equal("Unknown", PlatformHealthPresentation.GetOverallStatus(staleItems));
    }

    [Fact]
    public void BuildFreshItems_ForDegradedOrDownDependencies_EmitsInlineGuidance()
    {
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new(HealthStatus.Healthy, "ok", TimeSpan.Zero, null, null),
            ["stillops-ingestion"] = new(HealthStatus.Degraded, "slow", TimeSpan.Zero, null, null),
            ["stillops-identity-db"] = new(HealthStatus.Unhealthy, "down", TimeSpan.Zero, null, null)
        }, TimeSpan.Zero);

        IReadOnlyList<PlatformHealthItemViewModel> items =
            PlatformHealthPresentation.BuildFreshItems(report, DateTimeOffset.UtcNow);

        PlatformHealthItemViewModel ingestion = items.Single(item => item.Key == "stillops-ingestion");
        PlatformHealthItemViewModel identityDb = items.Single(item => item.Key == "stillops-identity-db");

        Assert.Equal("Degraded", ingestion.Status);
        Assert.NotNull(ingestion.Impact);
        Assert.NotNull(ingestion.NextAction);

        Assert.Equal("Down", identityDb.Status);
        Assert.NotNull(identityDb.Impact);
        Assert.NotNull(identityDb.NextAction);
    }
}
