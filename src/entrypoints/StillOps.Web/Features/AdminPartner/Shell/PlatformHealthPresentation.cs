namespace StillOps.Web.Features.AdminPartner.Shell;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed record PlatformHealthItemViewModel(
    string Key,
    string DisplayName,
    string Status,
    string TrustState,
    DateTimeOffset? LastChecked,
    string? Impact,
    string? NextAction);

public static class PlatformHealthPresentation
{
    private static readonly PlatformHealthDefinition[] s_monitoredItems =
    [
        new("self", "StillOps.Web"),
        new("stillops-ingestion", "StillOps.Ingestion"),
        new("stillops-identity-db", "PostgreSQL Identity Database")
    ];

    public static IReadOnlyList<PlatformHealthItemViewModel> BuildFreshItems(
        HealthReport report,
        DateTimeOffset lastChecked)
    {
        List<PlatformHealthItemViewModel> items = [];

        foreach (PlatformHealthDefinition definition in s_monitoredItems)
        {
            HealthStatus? status = report.Entries.TryGetValue(definition.Key, out HealthReportEntry entry)
                ? entry.Status
                : null;

            items.Add(BuildItem(
                definition,
                ToDisplayStatus(status),
                lastChecked,
                hasLastKnownStatus: status is not null,
                lastKnownStatus: status is not null ? ToDisplayStatus(status) : null));
        }

        return items;
    }

    public static IReadOnlyList<PlatformHealthItemViewModel> BuildUnavailableItems()
    {
        List<PlatformHealthItemViewModel> items = [];

        foreach (PlatformHealthDefinition definition in s_monitoredItems)
        {
            items.Add(BuildItem(
                definition,
                status: "Unknown",
                lastChecked: null,
                hasLastKnownStatus: false,
                lastKnownStatus: null));
        }

        return items;
    }

    public static IReadOnlyList<PlatformHealthItemViewModel> BuildStaleItems(
        IReadOnlyList<PlatformHealthItemViewModel> lastKnownItems)
    {
        if (lastKnownItems.Count == 0)
        {
            return BuildUnavailableItems();
        }

        List<PlatformHealthItemViewModel> items = [];

        foreach (PlatformHealthDefinition definition in s_monitoredItems)
        {
            PlatformHealthItemViewModel? priorItem = lastKnownItems
                .FirstOrDefault(item => string.Equals(item.Key, definition.Key, StringComparison.Ordinal));

            items.Add(BuildItem(
                definition,
                status: "Unknown",
                lastChecked: priorItem?.LastChecked,
                hasLastKnownStatus: priorItem is not null && !string.Equals(priorItem.Status, "Unknown", StringComparison.Ordinal),
                lastKnownStatus: priorItem?.Status));
        }

        return items;
    }

    public static string GetOverallStatus(IReadOnlyList<PlatformHealthItemViewModel> items)
    {
        if (items.Count == 0)
        {
            return "Unknown";
        }

        if (items.Any(item => string.Equals(item.Status, "Unknown", StringComparison.Ordinal)))
        {
            return "Unknown";
        }

        if (items.Any(item => string.Equals(item.Status, "Down", StringComparison.Ordinal)))
        {
            return "Down";
        }

        if (items.Any(item => string.Equals(item.Status, "Degraded", StringComparison.Ordinal)))
        {
            return "Degraded";
        }

        return "Healthy";
    }

    public static string GetSummaryMessage(string overallStatus, bool hasLastKnownState) => overallStatus switch
    {
        "Healthy" => "All starter services are available. Current status is suitable for routine operation.",
        "Degraded" => "One or more starter services are degraded. Some operations may be unreliable. Review the affected components below.",
        "Down" => "A starter service is unavailable. Safe operation cannot be assumed. Escalate if required.",
        _ when hasLastKnownState => "Health data is stale. Last known component status is shown inline below.",
        _ => "Health data is currently unavailable. No trusted status has been recorded yet."
    };

    public static string GetTrustState(string status, bool hasLastKnownState) => status switch
    {
        "Healthy" => "Trusted for routine operation.",
        "Degraded" => "Use caution and verify the affected workflow before proceeding.",
        "Down" => "Do not rely on this component for safe operation.",
        _ when hasLastKnownState => "Current data cannot be trusted until a fresh check succeeds.",
        _ => "Verification is required before relying on this component."
    };

    private static PlatformHealthItemViewModel BuildItem(
        PlatformHealthDefinition definition,
        string status,
        DateTimeOffset? lastChecked,
        bool hasLastKnownStatus,
        string? lastKnownStatus)
    {
        (string? impact, string? nextAction) = GetImpactAndAction(
            definition.DisplayName,
            status,
            hasLastKnownStatus,
            lastKnownStatus);

        return new PlatformHealthItemViewModel(
            definition.Key,
            definition.DisplayName,
            status,
            GetTrustState(status, hasLastKnownStatus),
            lastChecked,
            impact,
            nextAction);
    }

    private static string ToDisplayStatus(HealthStatus? status) => status switch
    {
        HealthStatus.Healthy => "Healthy",
        HealthStatus.Degraded => "Degraded",
        HealthStatus.Unhealthy => "Down",
        _ => "Unknown"
    };

    private static (string? Impact, string? NextAction) GetImpactAndAction(
        string displayName,
        string status,
        bool hasLastKnownStatus,
        string? lastKnownStatus)
    {
        if (string.Equals(status, "Healthy", StringComparison.Ordinal))
        {
            return (null, null);
        }

        if (string.Equals(status, "Unknown", StringComparison.Ordinal))
        {
            string impact = hasLastKnownStatus && lastKnownStatus is not null
                ? $"Current health data is stale. Last recorded status was {lastKnownStatus}."
                : "Current health data is unavailable for this component.";

            return (
                impact,
                "Refresh the view. If the state remains unknown, verify the dependency directly before making runtime changes.");
        }

        return displayName switch
        {
            "StillOps.Web" => string.Equals(status, "Degraded", StringComparison.Ordinal)
                ? ("The internal shell may respond slowly or intermittently.",
                    "Refresh the page. If degradation persists, check the application host before continuing.")
                : ("The internal shell is unavailable to operators.",
                    "Escalate to platform support immediately and avoid runtime changes."),

            "StillOps.Ingestion" => string.Equals(status, "Degraded", StringComparison.Ordinal)
                ? ("Live ingestion updates may be delayed or incomplete.",
                    "Review recent ingestion activity before relying on new telemetry.")
                : ("Live ingestion is unavailable and telemetry freshness cannot be trusted.",
                    "Escalate to platform support and avoid relying on new sensor data until recovery."),

            _ => string.Equals(status, "Degraded", StringComparison.Ordinal)
                ? ("The starter identity database is degraded and save or sign-in behavior may be unreliable.",
                    "Use caution with configuration changes and verify persistence before proceeding.")
                : ("The starter identity database is unavailable. Authentication and configuration persistence are at risk.",
                    "Escalate to platform support and avoid runtime changes until database health returns.")
        };
    }

    private sealed record PlatformHealthDefinition(string Key, string DisplayName);
}
