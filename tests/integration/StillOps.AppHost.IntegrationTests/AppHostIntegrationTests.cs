namespace StillOps.AppHost.IntegrationTests;

/// <summary>
/// Integration tests that start the full Aspire topology and verify Story 1.1 acceptance criteria.
/// Requires Docker or Aspire's local runner. No external infrastructure dependencies (no PostgreSQL/Redis).
/// </summary>
public sealed class AppHostIntegrationTests
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// AC 1, 2, 3 — The solution scaffolds from the approved Aspire baseline and the topology
    /// registers StillOps.Web and StillOps.Ingestion in a runnable starter topology.
    /// </summary>
    [Fact]
    public async Task AppHostStartsSuccessfully()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();

        await using var app = await appHost.BuildAsync()
            .WaitAsync(StartupTimeout);

        // Act
        await app.StartAsync()
            .WaitAsync(StartupTimeout);

        // Assert — both resources must reach Running state without throwing
        await app.ResourceNotifications
            .WaitForResourceAsync("stillops-web", KnownResourceStates.Running)
            .WaitAsync(StartupTimeout);

        await app.ResourceNotifications
            .WaitForResourceAsync("stillops-ingestion", KnownResourceStates.Running)
            .WaitAsync(StartupTimeout);
    }

    /// <summary>
    /// AC 4 — StillOps.Web exposes health/status registration consumable by the health view.
    /// The /health endpoint returns 200 OK. The sensor-connectivity check is registered
    /// (expected to be Degraded at starter topology — no live devices yet).
    /// </summary>
    [Fact]
    public async Task WebHealthEndpointReturnsOk()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();

        await using var app = await appHost.BuildAsync()
            .WaitAsync(StartupTimeout);

        await app.StartAsync()
            .WaitAsync(StartupTimeout);

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        using var httpClient = app.CreateHttpClient("stillops-web");

        // Act
        using var response = await httpClient.GetAsync("/health")
            .WaitAsync(RequestTimeout);

        // Assert
        // The health endpoint returns 200 OK when all checks are Healthy or Degraded.
        // sensor-connectivity is Degraded (expected for starter topology) but does not
        // cause a 503 because Degraded is treated as non-fatal by the default health check options.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("sensor-connectivity", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AC 5 — Anonymous users cannot reach internal shell content.
    /// An unauthenticated GET /shell is challenged with a redirect to /login.
    /// </summary>
    [Fact]
    public async Task WebShellRouteUnauthenticatedRequestRedirectsToLogin()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();

        await using var app = await appHost.BuildAsync()
            .WaitAsync(StartupTimeout);

        await app.StartAsync()
            .WaitAsync(StartupTimeout);

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        // Build a no-redirect HttpClient so we can inspect the raw 302 response.
        // CreateHttpClient resolves the service address; we take that base address
        // and construct a separate client that does not follow redirects.
        using var addressClient = app.CreateHttpClient("stillops-web");
        using var noRedirectClient = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = addressClient.BaseAddress,
        };

        // Act
        using var response = await noRedirectClient.GetAsync("/shell")
            .WaitAsync(RequestTimeout);

        // Assert — unauthenticated request must be challenged (redirect to /login)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/login", location, StringComparison.OrdinalIgnoreCase);
    }
}
