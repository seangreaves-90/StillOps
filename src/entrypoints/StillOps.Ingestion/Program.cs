using StillOps.Ingestion;
using StillOps.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults: OpenTelemetry, health checks ("self" liveness), and service discovery.
builder.AddServiceDefaults();

// ── Starter settings (shared reload source + DI binding) ─────────────────────
// Registers IOptionsMonitor<StarterSettings> from the shared STILLOPS_SETTINGS_FILE
// path set by AppHost (optional: true — Ingestion has no local seed file).
// When Web writes the shared file the file watcher fires and both hosts reload
// their IOptionsMonitor<StarterSettings> without restarting.
builder.AddStarterSettings();

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Expose liveness/readiness so the internal shell can monitor the worker directly.
app.MapDefaultEndpoints();

app.Run();
