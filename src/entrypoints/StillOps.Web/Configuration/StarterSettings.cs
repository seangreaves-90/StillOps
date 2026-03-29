namespace StillOps.Web.Configuration;

/// <summary>
/// Epic 1 starter settings set. Backed by a reload-capable JSON configuration source
/// so changes can become active without restarting the application (FR58 / AC 6).
/// Inject <see cref="Microsoft.Extensions.Options.IOptionsMonitor{T}"/> — not IOptions&lt;T&gt; —
/// to receive live-reloaded values.
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
