using StillOps.Ingestion;

var builder = Host.CreateApplicationBuilder(args);

// Aspire ServiceDefaults: OpenTelemetry, health checks ("self" liveness), service discovery.
builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
