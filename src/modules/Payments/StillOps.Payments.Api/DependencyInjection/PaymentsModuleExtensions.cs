using Microsoft.Extensions.Hosting;

namespace StillOps.Payments.Api.DependencyInjection;

public static class PaymentsModuleExtensions
{
    public static IHostApplicationBuilder AddPaymentsModule(this IHostApplicationBuilder builder)
    {
        // Domain services, application handlers, infrastructure persistence,
        // and endpoint registration will be added here by Epic 6 stories.
        return builder;
    }
}
