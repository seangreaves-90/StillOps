var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithHostPort(5433)
    .WithPgAdmin();

var identityDb = postgres.AddDatabase("stillops-identity");

var redis = builder.AddRedis("redis");

// Compute a shared settings file path that both the web host and the ingestion worker
// will read from. AppHost runs on the developer machine so LOCALAPPDATA is deterministic.
// When Web's StarterSettingsWriter saves to this path, the file watcher in both processes
// fires and IOptionsMonitor<StarterSettings> reloads without restarting either service.
string sharedSettingsFile = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "StillOps",
    "stillops-operational-settings.json");

var ingestion = builder.AddProject<Projects.StillOps_Ingestion>("stillops-ingestion")
    .WithEnvironment("STILLOPS_SETTINGS_FILE", sharedSettingsFile);

builder.AddProject<Projects.StillOps_Web>("stillops-web")
    .WithReference(identityDb)
    .WithReference(ingestion)
    .WithReference(redis)
    .WaitFor(identityDb)
    .WaitFor(ingestion)
    .WaitFor(redis)
    .WithEnvironment("STILLOPS_SETTINGS_FILE", sharedSettingsFile);

builder.Build().Run();
