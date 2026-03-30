using Microsoft.Extensions.Hosting;

namespace StillOps.Partner.Api.DependencyInjection;

public static class PartnerModuleExtensions
{
    public static IHostApplicationBuilder AddPartnerModule(this IHostApplicationBuilder builder)
    {
        // Domain services, application handlers, infrastructure persistence,
        // and endpoint registration will be added here by later epic stories.
        return builder;
    }
}
