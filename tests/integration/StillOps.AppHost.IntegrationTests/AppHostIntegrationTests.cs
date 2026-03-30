using System.Net.Http.Headers;
using System.Text.Json;

namespace StillOps.AppHost.IntegrationTests;

/// <summary>
/// Integration tests that start the full Aspire topology and verify acceptance criteria.
/// Requires Docker — the AppHost provisions PostgreSQL automatically via Aspire.Hosting.PostgreSQL.
///
/// Reusable pattern for future bounded-context integration tests:
///   1. Create the topology:  DistributedApplicationTestingBuilder.CreateAsync&lt;Projects.StillOps_AppHost&gt;()
///   2. Build and start:      appHost.BuildAsync() then app.StartAsync()
///   3. Wait for readiness:   app.ResourceNotifications.WaitForResourceHealthyAsync("stillops-web")
///   4. Create an HTTP client: app.CreateHttpClient("stillops-web")
///   5. Authenticate if needed using cookie or bearer token flows shown in existing tests.
/// Per-module integration test projects under tests/integration/ can reference the AppHost
/// project and follow this same pattern to test module-specific behavior against real infrastructure.
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
    /// AC 1, 2 — StillOps.Web exposes the starter topology health model consumed by the
    /// internal health view. The readiness endpoint reports the web host, ingestion worker,
    /// and starter database dependency.
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("self", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stillops-ingestion", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stillops-identity-db", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Story 1.2 AC 1 — Unauthenticated requests to /api routes return 401, not a redirect.
    /// The cookie auth OnRedirectToLogin event is overridden to write 401 for API paths
    /// so that API clients receive a machine-readable status rather than an HTML redirect.
    /// </summary>
    [Fact]
    public async Task ProtectedInternalApiReturns401ForUnauthenticatedRequest()
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

        // A no-redirect client is used so we inspect the raw status code rather than
        // following any redirect that would mask a misconfigured event handler.
        using var addressClient = app.CreateHttpClient("stillops-web");
        using var noRedirectClient = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = addressClient.BaseAddress,
        };

        // Act
        using var response = await noRedirectClient.GetAsync("/api/internal/workspace-status")
            .WaitAsync(RequestTimeout);

        // Assert — API route must return 401 Unauthorized, not 302 Found
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    /// <summary>
    /// Story 1.4 AC 1 — The protected health route keeps the same auth boundary as the shell.
    /// Anonymous requests to /shell/health must redirect to the sign-in page.
    /// </summary>
    [Fact]
    public async Task WebShellHealthRouteUnauthenticatedRequestRedirectsToLogin()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();

        await using var app = await appHost.BuildAsync()
            .WaitAsync(StartupTimeout);

        await app.StartAsync()
            .WaitAsync(StartupTimeout);

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        using var addressClient = app.CreateHttpClient("stillops-web");
        using var noRedirectClient = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = addressClient.BaseAddress,
        };

        using var response = await noRedirectClient.GetAsync("/shell/health")
            .WaitAsync(RequestTimeout);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/login", location, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Story 1.2 Review Finding — Bearer token issued by /connect/token grants access to
    /// /api/internal/workspace-status when the caller holds the required internal role.
    /// Verifies the OpenIddict validation scheme is now honoured by the InternalShell policy.
    /// </summary>
    [Fact]
    public async Task BearerTokenGrantsAccessToProtectedInternalApi()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();
        await using var app = await appHost.BuildAsync().WaitAsync(StartupTimeout);
        await app.StartAsync().WaitAsync(StartupTimeout);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        using var httpClient = app.CreateHttpClient("stillops-web");

        // Obtain a bearer token for the seeded operator user (InternalOperator role).
        using var tokenResponse = await httpClient.PostAsync("/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = "operator",
                ["password"] = "dev-only-change-me"
            }))
            .WaitAsync(RequestTimeout);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var accessToken = JsonDocument.Parse(tokenJson)
            .RootElement.GetProperty("access_token").GetString()!;

        // Act — call the protected API with the bearer token.
        using var apiRequest = new HttpRequestMessage(
            HttpMethod.Get, "/api/internal/workspace-status");
        apiRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        using var apiResponse = await httpClient.SendAsync(apiRequest).WaitAsync(RequestTimeout);

        // Assert — valid bearer token with the required role must return 200 OK.
        Assert.Equal(HttpStatusCode.OK, apiResponse.StatusCode);
    }

    /// <summary>
    /// Story 1.2 Review Finding — Bearer token with an insufficient role returns 403 Forbidden.
    /// The viewer user (role=ViewerRole) is authenticated but excluded from the InternalShell
    /// policy, so the API must reject the request with 403 rather than redirecting or returning 401.
    /// </summary>
    [Fact]
    public async Task BearerTokenWithWrongRoleReturns403OnProtectedInternalApi()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();
        await using var app = await appHost.BuildAsync().WaitAsync(StartupTimeout);
        await app.StartAsync().WaitAsync(StartupTimeout);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        using var baseClient = app.CreateHttpClient("stillops-web");
        using var noRedirectClient = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = baseClient.BaseAddress
        };

        // Obtain a bearer token for the seeded viewer user (ViewerRole — not in InternalShell).
        using var tokenResponse = await noRedirectClient.PostAsync("/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = "viewer",
                ["password"] = "dev-only-change-me"
            }))
            .WaitAsync(RequestTimeout);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var accessToken = JsonDocument.Parse(tokenJson)
            .RootElement.GetProperty("access_token").GetString()!;

        // Act — call the protected API with a token that carries the wrong role.
        using var apiRequest = new HttpRequestMessage(
            HttpMethod.Get, "/api/internal/workspace-status");
        apiRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        using var apiResponse = await noRedirectClient.SendAsync(apiRequest).WaitAsync(RequestTimeout);

        // Assert — wrong role must return 403 Forbidden, not a redirect.
        Assert.Equal(HttpStatusCode.Forbidden, apiResponse.StatusCode);
    }

    /// <summary>
    /// Story 1.2 Review Finding — The access-denied section in Routes.razor now renders a POST
    /// form (with AntiforgeryToken) instead of a GET link. Submitting the form signs the user
    /// out so they can retry with a different account (AC4).
    /// </summary>
    [Fact]
    public async Task AccessDeniedFormSignsOutUnauthorizedUserWhenSubmitted()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();
        await using var app = await appHost.BuildAsync().WaitAsync(StartupTimeout);
        await app.StartAsync().WaitAsync(StartupTimeout);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        var cookies = new System.Net.CookieContainer();
        using var baseClient = app.CreateHttpClient("stillops-web");
        using var client = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = cookies })
        {
            BaseAddress = baseClient.BaseAddress
        };

        // Step 1: GET /login to acquire the anonymous antiforgery token + cookie.
        using var loginPageResponse = await client.GetAsync("/login").WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, loginPageResponse.StatusCode);
        var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var loginToken = ExtractAntiforgeryToken(loginHtml);
        Assert.NotEmpty(loginToken);

        // Step 2: POST /login as the viewer user (authenticated but not in InternalShell policy).
        using var loginResponse = await client.PostAsync("/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Username"] = "viewer",
                ["Input.Password"] = "dev-only-change-me",
                ["__RequestVerificationToken"] = loginToken
            }))
            .WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        // Step 3: GET /shell — viewer is authenticated but unauthorized, so the cookie
        // middleware redirects the browser to /access-denied.
        using var shellResponse = await client.GetAsync("/shell").WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.Redirect, shellResponse.StatusCode);
        var accessDeniedLocation = shellResponse.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/access-denied", accessDeniedLocation, StringComparison.OrdinalIgnoreCase);

        // Step 4: GET /access-denied and extract the antiforgery token from the sign-out form.
        using var accessDeniedResponse = await client.GetAsync(accessDeniedLocation).WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, accessDeniedResponse.StatusCode);
        var accessDeniedHtml = await accessDeniedResponse.Content.ReadAsStringAsync();
        var logoutToken = ExtractAntiforgeryToken(accessDeniedHtml);
        Assert.NotEmpty(logoutToken);

        // Step 5: POST /logout using the antiforgery token rendered in the access-denied form.
        using var logoutResponse = await client.PostAsync("/logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutToken
            }))
            .WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        var logoutLocation = logoutResponse.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/login", logoutLocation, StringComparison.OrdinalIgnoreCase);

        // Step 6: Verify the session is invalidated — /shell now redirects to /login.
        using var afterLogoutResponse = await client.GetAsync("/shell").WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.Redirect, afterLogoutResponse.StatusCode);
        var afterLocation = afterLogoutResponse.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/login", afterLocation, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Story 1.3 AC 1 — An authenticated internal operator accessing /shell sees a starter
    /// shell with persistent primary navigation for Home, Health, and Configuration.
    /// Validates the navigation landmark, page heading, and sign-out control are present
    /// in the rendered HTML returned to the browser.
    /// </summary>
    [Fact]
    public async Task ShellRendersNavigationForAuthenticatedOperator()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();
        await using var app = await appHost.BuildAsync().WaitAsync(StartupTimeout);
        await app.StartAsync().WaitAsync(StartupTimeout);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        var cookies = new System.Net.CookieContainer();
        using var baseClient = app.CreateHttpClient("stillops-web");
        using var client = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = true, CookieContainer = cookies })
        {
            BaseAddress = baseClient.BaseAddress
        };

        // Step 1: GET /login to acquire the antiforgery token.
        using var loginPageResponse = await client.GetAsync("/login").WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, loginPageResponse.StatusCode);
        var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var loginToken = ExtractAntiforgeryToken(loginHtml);
        Assert.NotEmpty(loginToken);

        // Step 2: POST /login as the seeded operator user.
        using var loginResponse = await client.PostAsync("/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Username"] = "operator",
                ["Input.Password"] = "dev-only-change-me",
                ["__RequestVerificationToken"] = loginToken
            }))
            .WaitAsync(RequestTimeout);
        // Login redirects to /shell on success.
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Step 3: GET /shell and inspect the rendered navigation structure.
        using var shellResponse = await client.GetAsync("/shell").WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, shellResponse.StatusCode);

        var shellHtml = await shellResponse.Content.ReadAsStringAsync();

        // AC 1 — Persistent primary navigation must contain all three destinations.
        Assert.Contains("aria-label=\"Primary navigation\"", shellHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/shell\"", shellHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/shell/health\"", shellHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/shell/configuration\"", shellHtml, StringComparison.OrdinalIgnoreCase);

        // AC 1/2 — The shell keeps the current section title visible in the header and
        // the page still exposes a semantic h1 inside the main content.
        Assert.Contains("Current section", shellHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Workspace Home", shellHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<h1", shellHtml, StringComparison.OrdinalIgnoreCase);

        // AC 3 — Sign-out control must be present and reachable.
        Assert.Contains("btn-signout", shellHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/logout", shellHtml, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Story 1.4 AC 1, 2, 5 — An authenticated internal operator can access /shell/health
    /// and the page renders the starter topology with shared status language, trust state,
    /// and last-checked metadata in the shared shell wrapper.
    /// </summary>
    [Fact]
    public async Task HealthPageRendersForAuthenticatedOperator()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.StillOps_AppHost>();
        await using var app = await appHost.BuildAsync().WaitAsync(StartupTimeout);
        await app.StartAsync().WaitAsync(StartupTimeout);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("stillops-web")
            .WaitAsync(StartupTimeout);

        var cookies = new System.Net.CookieContainer();
        using var baseClient = app.CreateHttpClient("stillops-web");
        using var client = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = true, CookieContainer = cookies })
        {
            BaseAddress = baseClient.BaseAddress
        };

        // Step 1: GET /login and acquire the antiforgery token.
        using var loginPageResponse = await client.GetAsync("/login").WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, loginPageResponse.StatusCode);
        var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var loginToken = ExtractAntiforgeryToken(loginHtml);
        Assert.NotEmpty(loginToken);

        // Step 2: POST /login as the seeded operator user.
        using var loginResponse = await client.PostAsync("/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Username"] = "operator",
                ["Input.Password"] = "dev-only-change-me",
                ["__RequestVerificationToken"] = loginToken
            }))
            .WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Step 3: GET /shell/health and inspect the rendered content.
        using var healthResponse = await client.GetAsync("/shell/health").WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var healthHtml = await healthResponse.Content.ReadAsStringAsync();

        // AC 1 — Page renders with the correct heading.
        Assert.Contains("Platform Health", healthHtml, StringComparison.OrdinalIgnoreCase);

        // AC 1 / AC 4 — Shell navigation is present (surrounding context remains visible).
        Assert.Contains("aria-label=\"Primary navigation\"", healthHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/shell/health\"", healthHtml, StringComparison.OrdinalIgnoreCase);

        // AC 1 / AC 2 — The starter topology is rendered explicitly.
        Assert.Contains("StillOps.Web", healthHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("StillOps.Ingestion", healthHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PostgreSQL Identity Database", healthHtml, StringComparison.OrdinalIgnoreCase);

        // AC 1 / AC 5 — Status chips, trust state, and last-checked metadata are visible.
        Assert.Contains("status-chip", healthHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Trust state:", healthHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Last checked:", healthHtml, StringComparison.OrdinalIgnoreCase);

        // AC 2 — Shared status language reflects the healthy starter topology.
        Assert.Contains("Healthy", healthHtml, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        // Match name="__RequestVerificationToken" before value="..."
        var match = System.Text.RegularExpressions.Regex.Match(
            html,
            @"<input\b[^>]*\bname=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            // Match value="..." before name="__RequestVerificationToken"
            match = System.Text.RegularExpressions.Regex.Match(
                html,
                @"<input\b[^>]*\bvalue=""([^""]+)""[^>]*\bname=""__RequestVerificationToken""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return match.Groups[1].Value;
    }
}
