namespace StillOps.Web.Features.AdminPartner.Shell;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed partial class StillOpsIngestionHealthCheck(
    IHttpClientFactory httpClientFactory,
    ILogger<StillOpsIngestionHealthCheck> logger) : IHealthCheck
{
    public const string ClientName = "StillOpsIngestionHealth";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient(ClientName);
            using HttpResponseMessage response = await client.GetAsync("/alive", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("StillOps.Ingestion is reachable and reporting alive.");
            }

            return HealthCheckResult.Unhealthy(
                $"StillOps.Ingestion returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogHealthProbeFailed(logger, ex);

            return HealthCheckResult.Unhealthy(
                "StillOps.Ingestion could not be reached from the web host.",
                ex);
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "StillOps.Ingestion health probe failed.")]
    private static partial void LogHealthProbeFailed(
        ILogger logger,
        Exception exception);
}
