var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.StillOps_Web>("stillops-web");
builder.AddProject<Projects.StillOps_Ingestion>("stillops-ingestion");

builder.Build().Run();
