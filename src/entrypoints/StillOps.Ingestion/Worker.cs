namespace StillOps.Ingestion;

/// <summary>
/// Starter ingestion worker. Story 1.1 scope: placeholder that confirms the
/// worker host starts, is orchestrated by Aspire, and exposes health status.
/// Ingestion pipeline logic arrives in Epic 3 (FR15–FR22).
/// </summary>
public sealed partial class Worker(ILogger<Worker> logger) : BackgroundService
{
    private static readonly TimeSpan HealthReportInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            LogWorkerRunning(logger);
            await Task.Delay(HealthReportInterval, stoppingToken);
        }

        LogWorkerStopping(logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "StillOps.Ingestion worker started.")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "StillOps.Ingestion worker running. No live device traffic — starter topology only.")]
    private static partial void LogWorkerRunning(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "StillOps.Ingestion worker stopping.")]
    private static partial void LogWorkerStopping(ILogger logger);
}
