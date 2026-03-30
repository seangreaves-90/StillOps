using Microsoft.Extensions.Options;

using StillOps.BuildingBlocks.Time;
using StillOps.ServiceDefaults.Configuration;
using StillOps.Web.Configuration;

namespace StillOps.AppHost.IntegrationTests;

/// <summary>
/// Tests for Story 1.5 — starter runtime configuration without restart.
/// Unit tests (Tasks 1–8) instantiate <see cref="StarterSettingsWriter"/> and
/// <see cref="StarterSettingsValidator"/> directly; no Aspire stack is required.
/// The integration test (Task 9) starts the full topology and verifies the
/// configuration page renders with an editable form for an authenticated operator.
/// </summary>
public sealed class StarterSettingsConfigurationTests
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    // ── Helper ──────────────────────────────────────────────────────────────────

    private static StarterSettingsWriter CreateWriter(string filePath) =>
        new(Options.Create(new StarterSettingsWriterOptions { FilePath = filePath }),
            new SystemDateTimeProvider());

    private static StarterSettingsWriter CreateWriter(string filePath, IDateTimeProvider dateTimeProvider) =>
        new(Options.Create(new StarterSettingsWriterOptions { FilePath = filePath }),
            dateTimeProvider);

    private static StarterSettingsFormModel ValidModel() => new()
    {
        TemperatureCelsius = 85.0,
        HumidityPercent = 80.0,
        AbvPercent = 95.0,
        SensorHeartbeatTimeoutSeconds = 30,
        AlertOnMissedHeartbeats = true,
        AlertOnThresholdExceedance = true
    };

    // ── AC 2, 3 — Successful save persists updated values ───────────────────────

    /// <summary>
    /// AC 2, 3 — SaveAsync with a valid model writes a JSON file that contains the
    /// new field values so the file-based IOptionsMonitor reload picks them up.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ValidModel_WritesFileWithUpdatedValues()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            var writer = CreateWriter(filePath);
            var model = ValidModel();
            model.SensorHeartbeatTimeoutSeconds = 120;
            model.TemperatureCelsius = 90.5;

            await writer.SaveAsync(model, "test-user");

            var json = await File.ReadAllTextAsync(filePath);
            Assert.Contains("120", json, StringComparison.Ordinal);
            Assert.Contains("90.5", json, StringComparison.Ordinal);
            Assert.Contains("StarterSettings", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// AC 3 — SaveAsync returns a success result carrying the save timestamp and username
    /// so the Razor component can render last-updated confirmation metadata.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ValidModel_ReturnsSuccessResultWithMetadata()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            var writer = CreateWriter(filePath);
            DateTimeOffset before = DateTimeOffset.UtcNow;

            SaveResult result = await writer.SaveAsync(ValidModel(), "ops-admin");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.SavedAtUtc);
            Assert.True(result.SavedAtUtc!.Value >= before);
            Assert.Equal("ops-admin", result.SavedBy);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    // ── AC 5 — Failure state returns a result, never throws ────────────────────

    /// <summary>
    /// AC 5 — When the settings file cannot be written, SaveAsync returns a Failure
    /// result with an error message so the Razor component can preserve entered values
    /// and display the failure state inline without catching exceptions.
    /// </summary>
    [Fact]
    public async Task SaveAsync_UnwritablePath_ReturnsFailureResult()
    {
        // Create a directory at the exact path where the writer's temp file would go
        // (.tmp suffix).  WriteAllTextAsync to a directory path throws IOException (Linux)
        // or UnauthorizedAccessException (Windows), both caught by the writer.
        string tempDir = Directory.CreateTempSubdirectory("stillops-test-").FullName;
        string settingsPath = Path.Combine(tempDir, "settings.json");
        string tmpBlocker = settingsPath + ".tmp";
        Directory.CreateDirectory(tmpBlocker);

        try
        {
            var writer = CreateWriter(settingsPath);

            SaveResult result = await writer.SaveAsync(ValidModel(), "test-user");

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.NotEmpty(result.ErrorMessage!);
        }
        finally
        {
            // Clean up the blocking directory and temp folder.
            Directory.Delete(tmpBlocker);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ── AC 4 — Validation blocks invalid input ──────────────────────────────────

    /// <summary>
    /// AC 4 — ValidateAll returns no errors for a model with all fields exactly at
    /// their boundary values, proving the validator accepts valid extremes.
    /// </summary>
    [Fact]
    public void ValidateAll_BoundaryValues_ReturnsNoErrors()
    {
        var model = new StarterSettingsFormModel
        {
            TemperatureCelsius = 0.0,           // min
            HumidityPercent = 100.0,            // max
            AbvPercent = 0.0,                   // min
            SensorHeartbeatTimeoutSeconds = 3600 // max
        };

        Dictionary<string, string> errors = StarterSettingsValidator.ValidateAll(model);

        Assert.Empty(errors);
    }

    /// <summary>
    /// AC 4 — ValidateAll flags TemperatureCelsius when the value exceeds 200 °C.
    /// </summary>
    [Fact]
    public void ValidateAll_TemperatureCelsiusAboveMaximum_ReturnsFieldError()
    {
        var model = ValidModel();
        model.TemperatureCelsius = 200.1;

        Dictionary<string, string> errors = StarterSettingsValidator.ValidateAll(model);

        Assert.True(errors.ContainsKey(nameof(StarterSettingsFormModel.TemperatureCelsius)));
    }

    /// <summary>
    /// AC 4 — ValidateAll flags HumidityPercent when the value is below 0%.
    /// </summary>
    [Fact]
    public void ValidateAll_HumidityPercentBelowMinimum_ReturnsFieldError()
    {
        var model = ValidModel();
        model.HumidityPercent = -0.1;

        Dictionary<string, string> errors = StarterSettingsValidator.ValidateAll(model);

        Assert.True(errors.ContainsKey(nameof(StarterSettingsFormModel.HumidityPercent)));
    }

    /// <summary>
    /// AC 4 — ValidateAll flags AbvPercent when the value exceeds 100%.
    /// </summary>
    [Fact]
    public void ValidateAll_AbvPercentAboveMaximum_ReturnsFieldError()
    {
        var model = ValidModel();
        model.AbvPercent = 100.1;

        Dictionary<string, string> errors = StarterSettingsValidator.ValidateAll(model);

        Assert.True(errors.ContainsKey(nameof(StarterSettingsFormModel.AbvPercent)));
    }

    /// <summary>
    /// AC 4 — ValidateAll flags SensorHeartbeatTimeoutSeconds when the value is 0
    /// (below the minimum of 1 second).
    /// </summary>
    [Fact]
    public void ValidateAll_HeartbeatTimeoutZero_ReturnsFieldError()
    {
        var model = ValidModel();
        model.SensorHeartbeatTimeoutSeconds = 0;

        Dictionary<string, string> errors = StarterSettingsValidator.ValidateAll(model);

        Assert.True(errors.ContainsKey(nameof(StarterSettingsFormModel.SensorHeartbeatTimeoutSeconds)));
    }

    // ── AC 1 — Configuration page restricted to administrators ──────────────────

    /// <summary>
    /// AC 1 — An authenticated internal administrator can navigate to /shell/configuration
    /// and the server-side rendered HTML contains an editable form with the three
    /// starter settings fieldsets within the shared shell wrapper.
    /// The configuration page is restricted to the InternalAdmin role.
    /// </summary>
    [Fact]
    public async Task ConfigurationPage_AuthenticatedAdmin_RendersEditableForm()
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

        // Step 2: POST /login as the seeded admin user (InternalAdmin role).
        using var loginResponse = await client.PostAsync("/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Username"] = "admin",
                ["Input.Password"] = "dev-only-change-me",
                ["__RequestVerificationToken"] = loginToken
            }))
            .WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Step 3: GET /shell/configuration and verify the editable form renders.
        using var configResponse = await client.GetAsync("/shell/configuration")
            .WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, configResponse.StatusCode);

        var configHtml = await configResponse.Content.ReadAsStringAsync();

        // AC 1 — Page must have the editable form with the known aria label.
        Assert.Contains("Starter settings form", configHtml, StringComparison.OrdinalIgnoreCase);

        // AC 1 — All three starter settings fieldsets must be present.
        Assert.Contains("Sensor Warning Thresholds", configHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sensor Heartbeat Timeout", configHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Starter Alerting Rules", configHtml, StringComparison.OrdinalIgnoreCase);

        // AC 1 — Shell navigation context must remain visible on the configuration page.
        Assert.Contains("aria-label=\"Primary navigation\"", configHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/shell/configuration\"", configHtml, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AC 1 — An authenticated internal operator (InternalOperator role) is denied access
    /// to /shell/configuration because the page is restricted to administrators.
    /// The browser receives an access-denied response rather than the form.
    /// </summary>
    [Fact]
    public async Task ConfigurationPage_AuthenticatedOperator_IsAccessDenied()
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

        // Step 2: POST /login as the seeded operator user (InternalOperator role, not InternalAdmin).
        using var loginResponse = await client.PostAsync("/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Username"] = "operator",
                ["Input.Password"] = "dev-only-change-me",
                ["__RequestVerificationToken"] = loginToken
            }))
            .WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Step 3: GET /shell/configuration — operator must be denied.
        // Blazor AuthorizeRouteView renders the NotAuthorized slot (access-denied HTML)
        // rather than the configuration form, so the response is 200 but the form is absent.
        using var configResponse = await client.GetAsync("/shell/configuration")
            .WaitAsync(RequestTimeout);
        Assert.Equal(HttpStatusCode.OK, configResponse.StatusCode);

        var configHtml = await configResponse.Content.ReadAsStringAsync();

        // The editable form must NOT be present for the operator.
        Assert.DoesNotContain("Starter settings form", configHtml, StringComparison.OrdinalIgnoreCase);
    }

    // ── AC 3 — Saved metadata is readable from the active runtime settings ───────

    // ── AC 2, 3 — Deterministic time via IDateTimeProvider ────────────────────

    /// <summary>
    /// AC 2, 3, BuildingBlocks bridge — SaveAsync stamps metadata using the injected
    /// IDateTimeProvider rather than DateTimeOffset.UtcNow, proving the shared time
    /// abstraction from StillOps.BuildingBlocks is actually consumed at runtime.
    /// A fixed-time provider lets the test assert an exact timestamp.
    /// </summary>
    [Fact]
    public async Task SaveAsync_WithFixedTimeProvider_StampsExactTimestamp()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            var fixedTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
            var writer = CreateWriter(filePath, new FakeDateTimeProvider(fixedTime));

            SaveResult result = await writer.SaveAsync(ValidModel(), "bridge-test");

            Assert.True(result.IsSuccess);
            Assert.Equal(fixedTime, result.SavedAtUtc);

            var json = await File.ReadAllTextAsync(filePath);
            Assert.Contains("2026-06-15", json, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// AC 3 — After a successful save, the written JSON file carries LastUpdatedUtc and
    /// LastUpdatedBy under the StarterSettings section. When deserialized via the
    /// IOptionsMonitor reload path the metadata is present, proving the configuration page
    /// can display it after a page reload without relying on transient in-memory state.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ValidModel_WrittenMetadataDeserializesFromReloadedJson()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            var writer = CreateWriter(filePath);
            const string savedBy = "ops-admin";

            await writer.SaveAsync(ValidModel(), savedBy);

            // Read the written JSON — this is exactly what IOptionsMonitor's file watcher
            // would pick up on the next reload cycle.
            var json = await File.ReadAllTextAsync(filePath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var settingsSection = doc.RootElement.GetProperty("StarterSettings");

            Assert.True(settingsSection.TryGetProperty("LastUpdatedUtc", out var utcEl),
                "LastUpdatedUtc must be present in the written JSON for IOptionsMonitor reload.");
            Assert.True(settingsSection.TryGetProperty("LastUpdatedBy", out var byEl),
                "LastUpdatedBy must be present in the written JSON for IOptionsMonitor reload.");

            // UTC value must be parseable as a DateTimeOffset (the format IOptions uses).
            Assert.True(DateTimeOffset.TryParse(utcEl.GetString(), out _),
                "LastUpdatedUtc must be a valid DateTimeOffset string.");
            Assert.Equal(savedBy, byEl.GetString());
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    // ── Test doubles ────────────────────────────────────────────────────────────

    private sealed class FakeDateTimeProvider(DateTimeOffset fixedUtcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => fixedUtcNow;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            html,
            @"<input\b[^>]*\bname=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = System.Text.RegularExpressions.Regex.Match(
                html,
                @"<input\b[^>]*\bvalue=""([^""]+)""[^>]*\bname=""__RequestVerificationToken""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return match.Groups[1].Value;
    }
}
