using StillOps.Commerce.Api.DependencyInjection;
using StillOps.Distillery.Api.DependencyInjection;
using StillOps.Partner.Api.DependencyInjection;
using StillOps.Payments.Api.DependencyInjection;

namespace StillOps.Web.Composition;

/// <summary>
/// Single composition root for bounded-context module registration.
/// All module DI wiring flows through this method — do not register
/// module services directly in Program.cs or other entrypoints.
/// Each module exposes its registrar in its Api layer's DependencyInjection folder.
/// </summary>
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
