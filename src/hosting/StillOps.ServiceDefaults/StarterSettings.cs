namespace StillOps.ServiceDefaults.Configuration;

/// <summary>
/// Epic 1 starter settings set. Shared across all services that need read access to
/// the runtime configuration. Backed by a reload-capable JSON configuration source so
/// changes become active without restarting either the web host or the ingestion worker.
/// Inject <see cref="Microsoft.Extensions.Options.IOptionsMonitor{T}"/> — not
/// IOptions&lt;T&gt; — to receive live-reloaded values.
/// </summary>
public sealed class StarterSettings
{
    public const string SectionName = "StarterSettings";

    public SensorWarningThresholds SensorWarningThresholds { get; init; } = new();

    /// <summary>
    /// Number of seconds after the last heartbeat before a sensor is considered unresponsive.
    /// </summary>
    public int SensorHeartbeatTimeoutSeconds { get; init; } = 30;

    public StarterAlertingRules StarterAlertingRules { get; init; } = new();

    /// <summary>
    /// Set by <c>StarterSettingsWriter</c> on every successful save. Populated in the
    /// persisted JSON file so both the web host and ingestion worker receive the metadata
    /// after their <see cref="Microsoft.Extensions.Options.IOptionsMonitor{T}"/> reloads.
    /// </summary>
    public DateTimeOffset? LastUpdatedUtc { get; init; }

    /// <summary>Username of the administrator who last saved these settings.</summary>
    public string? LastUpdatedBy { get; init; }
}

public sealed class SensorWarningThresholds
{
    /// <summary>Celsius — warn above this temperature reading.</summary>
    public double TemperatureCelsius { get; init; } = 85.0;

    /// <summary>Relative humidity percentage — warn above this value.</summary>
    public double HumidityPercent { get; init; } = 80.0;

    /// <summary>ABV percentage — warn above this value during active distillation.</summary>
    public double AbvPercent { get; init; } = 95.0;
}

public sealed class StarterAlertingRules
{
    /// <summary>Raise an alert when a sensor misses its configured heartbeat window.</summary>
    public bool AlertOnMissedHeartbeats { get; init; } = true;

    /// <summary>Raise an alert when a sensor reading exceeds a warning threshold.</summary>
    public bool AlertOnThresholdExceedance { get; init; } = true;
}
