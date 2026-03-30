using Microsoft.Extensions.Hosting;

namespace StillOps.Commerce.Api.DependencyInjection;

public static class CommerceModuleExtensions
{
    public static IHostApplicationBuilder AddCommerceModule(this IHostApplicationBuilder builder)
    {
        // Domain services, application handlers, infrastructure persistence,
        // and endpoint registration will be added here by later epic stories.
        return builder;
    }
}
