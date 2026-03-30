using System.Text.Json;

using Microsoft.Extensions.Options;

using StillOps.ServiceDefaults.Configuration;

namespace StillOps.Web.Configuration;

// ── Form model ────────────────────────────────────────────────────────────────

/// <summary>
/// Mutable flat POCO bound bidirectionally to the configuration form in
/// <c>ShellConfiguration.razor</c>. Created from the current
/// <see cref="StarterSettings"/> snapshot and submitted to
/// <see cref="IStarterSettingsWriter.SaveAsync"/>.
/// </summary>
public sealed class StarterSettingsFormModel
{
    public double TemperatureCelsius { get; set; }
    public double HumidityPercent { get; set; }
    public double AbvPercent { get; set; }
    public int SensorHeartbeatTimeoutSeconds { get; set; }
    public bool AlertOnMissedHeartbeats { get; set; }
    public bool AlertOnThresholdExceedance { get; set; }

    /// <summary>
    /// Factory — always called from the Razor component's <c>OnInitialized</c> and
    /// reset path to load the current live values from <see cref="StarterSettings"/>.
    /// </summary>
    public static StarterSettingsFormModel FromSettings(StarterSettings s) => new()
    {
        TemperatureCelsius = s.SensorWarningThresholds.TemperatureCelsius,
        HumidityPercent = s.SensorWarningThresholds.HumidityPercent,
        AbvPercent = s.SensorWarningThresholds.AbvPercent,
        SensorHeartbeatTimeoutSeconds = s.SensorHeartbeatTimeoutSeconds,
        AlertOnMissedHeartbeats = s.StarterAlertingRules.AlertOnMissedHeartbeats,
        AlertOnThresholdExceedance = s.StarterAlertingRules.AlertOnThresholdExceedance
    };
}

// ── Validator ─────────────────────────────────────────────────────────────────

/// <summary>
/// Stateless, synchronous validation for <see cref="StarterSettingsFormModel"/>.
/// Each per-field method returns <see langword="null"/> when valid or an
/// English error string when invalid. <see cref="ValidateAll"/> aggregates all
/// failing fields into a dictionary keyed by the property name, suitable for
/// inline rendering in the Razor form.
/// </summary>
public static class StarterSettingsValidator
{
    public static string? ValidateTemperatureCelsius(double value) =>
        value is >= 0.0 and <= 200.0
            ? null
            : "Temperature must be between 0 °C and 200 °C.";

    public static string? ValidateHumidityPercent(double value) =>
        value is >= 0.0 and <= 100.0
            ? null
            : "Humidity must be between 0% and 100%.";

    public static string? ValidateAbvPercent(double value) =>
        value is >= 0.0 and <= 100.0
            ? null
            : "ABV must be between 0% and 100%.";

    public static string? ValidateSensorHeartbeatTimeoutSeconds(int value) =>
        value is >= 1 and <= 3600
            ? null
            : "Heartbeat timeout must be between 1 and 3600 seconds.";

    /// <summary>
    /// Returns a dictionary of property name → error message for all failing fields.
    /// An empty dictionary means the model is fully valid.
    /// </summary>
    public static Dictionary<string, string> ValidateAll(StarterSettingsFormModel model)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        void Check(string key, string? error)
        {
            if (error is not null)
            {
                errors[key] = error;
            }
        }

        Check(nameof(StarterSettingsFormModel.TemperatureCelsius),
            ValidateTemperatureCelsius(model.TemperatureCelsius));
        Check(nameof(StarterSettingsFormModel.HumidityPercent),
            ValidateHumidityPercent(model.HumidityPercent));
        Check(nameof(StarterSettingsFormModel.AbvPercent),
            ValidateAbvPercent(model.AbvPercent));
        Check(nameof(StarterSettingsFormModel.SensorHeartbeatTimeoutSeconds),
            ValidateSensorHeartbeatTimeoutSeconds(model.SensorHeartbeatTimeoutSeconds));

        // Boolean fields have no invalid state; they are omitted.
        return errors;
    }
}

// ── Save result ───────────────────────────────────────────────────────────────

/// <summary>
/// Immutable result returned by <see cref="IStarterSettingsWriter.SaveAsync"/>.
/// The Razor component inspects <see cref="IsSuccess"/> to choose the success
/// or failure feedback banner, never catching exceptions from the writer.
/// </summary>
public sealed class SaveResult
{
    private SaveResult() { }

    public bool IsSuccess { get; private init; }
    public DateTimeOffset? SavedAtUtc { get; private init; }
    public string? SavedBy { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static SaveResult Success(DateTimeOffset savedAtUtc, string savedBy) =>
        new() { IsSuccess = true, SavedAtUtc = savedAtUtc, SavedBy = savedBy };

    public static SaveResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

// ── Writer interface and options ──────────────────────────────────────────────

/// <summary>
/// Options for <see cref="StarterSettingsWriter"/>. Configured in
/// <c>Program.cs</c> with the resolved settings file path so the writer
/// never takes an <c>IWebHostEnvironment</c> dependency.
/// </summary>
public sealed class StarterSettingsWriterOptions
{
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Persists a validated <see cref="StarterSettingsFormModel"/> to the settings
/// JSON file, triggering a live reload in all consumers of
/// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{StarterSettings}"/>
/// without restarting the application.
/// </summary>
public interface IStarterSettingsWriter
{
    /// <summary>
    /// Validates, serializes, and atomically writes the settings model to the
    /// configured file path. Returns a <see cref="SaveResult"/> — never throws
    /// on I/O failure so callers can present inline error state.
    /// </summary>
    Task<SaveResult> SaveAsync(
        StarterSettingsFormModel model,
        string savedBy,
        CancellationToken cancellationToken = default);
}

// ── Writer implementation ─────────────────────────────────────────────────────

/// <inheritdoc cref="IStarterSettingsWriter"/>
public sealed class StarterSettingsWriter(IOptions<StarterSettingsWriterOptions> options) : IStarterSettingsWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public async Task<SaveResult> SaveAsync(
        StarterSettingsFormModel model,
        string savedBy,
        CancellationToken cancellationToken = default)
    {
        // Defense-in-depth: the Razor component validates before calling this method,
        // but the writer is the trust boundary for any programmatic callers.
        Dictionary<string, string> errors = StarterSettingsValidator.ValidateAll(model);
        if (errors.Count > 0)
        {
            return SaveResult.Failure("One or more fields are invalid. No changes were saved.");
        }

        DateTimeOffset savedAtUtc = DateTimeOffset.UtcNow;

        // Build the JSON payload preserving the existing section key name.
        var payload = new
        {
            StarterSettings = new
            {
                SensorWarningThresholds = new
                {
                    model.TemperatureCelsius,
                    model.HumidityPercent,
                    model.AbvPercent
                },
                model.SensorHeartbeatTimeoutSeconds,
                StarterAlertingRules = new
                {
                    model.AlertOnMissedHeartbeats,
                    model.AlertOnThresholdExceedance
                },
                LastUpdatedUtc = savedAtUtc,
                LastUpdatedBy = savedBy
            }
        };

        try
        {
            string filePath = options.Value.FilePath;

            // Ensure the target directory exists — required on first run when AppHost
            // has pointed both services at a shared LOCALAPPDATA path that hasn't been
            // written to yet.
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(payload, SerializerOptions);

            // Atomic write via temp file + rename prevents the IOptionsMonitor file
            // watcher from observing a partial write.
            string tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, filePath, overwrite: true);

            return SaveResult.Success(savedAtUtc, savedBy);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return SaveResult.Failure(
                "The settings file could not be saved. " +
                "Verify that the process has write access to the settings path, then try again.");
        }
    }
}
