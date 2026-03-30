using StillOps.Commerce.Api.DependencyInjection;
using StillOps.Distillery.Api.DependencyInjection;
using StillOps.Partner.Api.DependencyInjection;
using StillOps.Payments.Api.DependencyInjection;

namespace StillOps.Web.Composition;

public static class ModuleComposition
{
    public static IHostApplicationBuilder AddStillOpsModules(this IHostApplicationBuilder builder)
    {
        builder.AddDistilleryModule();
        builder.AddCommerceModule();
        builder.AddPaymentsModule();
        builder.AddPartnerModule();
        return builder;
    }
}
