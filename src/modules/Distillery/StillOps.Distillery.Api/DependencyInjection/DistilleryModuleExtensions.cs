using Microsoft.Extensions.Hosting;

namespace StillOps.Distillery.Api.DependencyInjection;

public static class DistilleryModuleExtensions
{
    public static IHostApplicationBuilder AddDistilleryModule(this IHostApplicationBuilder builder)
    {
        // Domain services, application handlers, infrastructure persistence,
        // and endpoint registration will be added here by Epic 2 stories.
        return builder;
    }
}
