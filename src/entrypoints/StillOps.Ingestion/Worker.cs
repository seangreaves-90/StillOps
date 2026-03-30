using Microsoft.Extensions.Options;

using StillOps.ServiceDefaults.Configuration;

namespace StillOps.Ingestion;

/// <summary>
/// Starter ingestion worker. Story 1.1 scope: placeholder that confirms the
/// worker host starts, is orchestrated by Aspire, and exposes health status.
/// Story 1.5: consumes <see cref="IOptionsMonitor{StarterSettings}"/> to prove
/// no-restart activation — settings written by the web host reload here without
/// restarting this process.
/// Ingestion pipeline logic arrives in Epic 3 (FR15–FR22).
/// </summary>
public sealed partial class Worker(
    ILogger<Worker> logger,
    IOptionsMonitor<StarterSettings> settingsMonitor) : BackgroundService
{
    private static readonly TimeSpan HealthReportInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        // Register a change listener so an IOptionsMonitor reload (triggered by the
        // web host writing the shared settings file) produces a visible log entry.
        // The IDisposable registration is held for the lifetime of ExecuteAsync.
        using IDisposable? changeRegistration = settingsMonitor.OnChange(updated =>
            LogSettingsChanged(logger, updated.SensorHeartbeatTimeoutSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            StarterSettings current = settingsMonitor.CurrentValue;
            LogWorkerRunning(
                logger,
                current.SensorHeartbeatTimeoutSeconds,
                current.StarterAlertingRules.AlertOnMissedHeartbeats,
                current.StarterAlertingRules.AlertOnThresholdExceedance);

            await Task.Delay(HealthReportInterval, stoppingToken);
        }

        LogWorkerStopping(logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "StillOps.Ingestion worker started.")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "StillOps.Ingestion worker running. HeartbeatTimeout={HeartbeatTimeoutSeconds}s " +
                  "AlertMissedHeartbeats={AlertMissedHeartbeats} AlertThreshold={AlertThreshold}.")]
    private static partial void LogWorkerRunning(
        ILogger logger,
        int heartbeatTimeoutSeconds,
        bool alertMissedHeartbeats,
        bool alertThreshold);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "StillOps.Ingestion detected settings reload. New HeartbeatTimeout={HeartbeatTimeoutSeconds}s.")]
    private static partial void LogSettingsChanged(ILogger logger, int heartbeatTimeoutSeconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "StillOps.Ingestion worker stopping.")]
    private static partial void LogWorkerStopping(ILogger logger);
}
