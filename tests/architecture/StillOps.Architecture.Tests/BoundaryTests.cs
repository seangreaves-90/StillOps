using System.Reflection;

using NetArchTest.Rules;

namespace StillOps.Architecture.Tests;

public class BoundaryTests
{
    [Fact]
    public void DomainProjects_ShouldNotReference_InfrastructureOrApiProjects()
    {
        string[] forbiddenAssemblies =
        [
            "StillOps.Distillery.Infrastructure",
            "StillOps.Commerce.Infrastructure",
            "StillOps.Payments.Infrastructure",
            "StillOps.Partner.Infrastructure",
            "StillOps.Distillery.Api",
            "StillOps.Commerce.Api",
            "StillOps.Payments.Api",
            "StillOps.Partner.Api"
        ];

        var distilleryResult = Types.InAssembly(Assembly.Load("StillOps.Distillery.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(distilleryResult.IsSuccessful,
            $"StillOps.Distillery.Domain has forbidden dependencies: {string.Join(", ", distilleryResult.FailingTypeNames ?? [])}");

        var commerceResult = Types.InAssembly(Assembly.Load("StillOps.Commerce.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(commerceResult.IsSuccessful,
            $"StillOps.Commerce.Domain has forbidden dependencies: {string.Join(", ", commerceResult.FailingTypeNames ?? [])}");

        var paymentsResult = Types.InAssembly(Assembly.Load("StillOps.Payments.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(paymentsResult.IsSuccessful,
            $"StillOps.Payments.Domain has forbidden dependencies: {string.Join(", ", paymentsResult.FailingTypeNames ?? [])}");

        var partnerResult = Types.InAssembly(Assembly.Load("StillOps.Partner.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(partnerResult.IsSuccessful,
            $"StillOps.Partner.Domain has forbidden dependencies: {string.Join(", ", partnerResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void DomainProjects_ShouldNotReference_OtherModuleDomains()
    {
        var distilleryResult = Types.InAssembly(Assembly.Load("StillOps.Distillery.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Commerce.Domain",
                "StillOps.Payments.Domain",
                "StillOps.Partner.Domain")
            .GetResult();
        Assert.True(distilleryResult.IsSuccessful,
            $"StillOps.Distillery.Domain has forbidden peer dependencies: {string.Join(", ", distilleryResult.FailingTypeNames ?? [])}");

        var commerceResult = Types.InAssembly(Assembly.Load("StillOps.Commerce.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Domain",
                "StillOps.Payments.Domain",
                "StillOps.Partner.Domain")
            .GetResult();
        Assert.True(commerceResult.IsSuccessful,
            $"StillOps.Commerce.Domain has forbidden peer dependencies: {string.Join(", ", commerceResult.FailingTypeNames ?? [])}");

        var paymentsResult = Types.InAssembly(Assembly.Load("StillOps.Payments.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Domain",
                "StillOps.Commerce.Domain",
                "StillOps.Partner.Domain")
            .GetResult();
        Assert.True(paymentsResult.IsSuccessful,
            $"StillOps.Payments.Domain has forbidden peer dependencies: {string.Join(", ", paymentsResult.FailingTypeNames ?? [])}");

        var partnerResult = Types.InAssembly(Assembly.Load("StillOps.Partner.Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Domain",
                "StillOps.Commerce.Domain",
                "StillOps.Payments.Domain")
            .GetResult();
        Assert.True(partnerResult.IsSuccessful,
            $"StillOps.Partner.Domain has forbidden peer dependencies: {string.Join(", ", partnerResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void ApplicationProjects_ShouldNotReference_OtherModuleApplications()
    {
        var distilleryResult = Types.InAssembly(Assembly.Load("StillOps.Distillery.Application"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Commerce.Application",
                "StillOps.Payments.Application",
                "StillOps.Partner.Application")
            .GetResult();
        Assert.True(distilleryResult.IsSuccessful,
            $"StillOps.Distillery.Application has forbidden peer dependencies: {string.Join(", ", distilleryResult.FailingTypeNames ?? [])}");

        var commerceResult = Types.InAssembly(Assembly.Load("StillOps.Commerce.Application"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Application",
                "StillOps.Payments.Application",
                "StillOps.Partner.Application")
            .GetResult();
        Assert.True(commerceResult.IsSuccessful,
            $"StillOps.Commerce.Application has forbidden peer dependencies: {string.Join(", ", commerceResult.FailingTypeNames ?? [])}");

        var paymentsResult = Types.InAssembly(Assembly.Load("StillOps.Payments.Application"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Application",
                "StillOps.Commerce.Application",
                "StillOps.Partner.Application")
            .GetResult();
        Assert.True(paymentsResult.IsSuccessful,
            $"StillOps.Payments.Application has forbidden peer dependencies: {string.Join(", ", paymentsResult.FailingTypeNames ?? [])}");

        var partnerResult = Types.InAssembly(Assembly.Load("StillOps.Partner.Application"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Application",
                "StillOps.Commerce.Application",
                "StillOps.Payments.Application")
            .GetResult();
        Assert.True(partnerResult.IsSuccessful,
            $"StillOps.Partner.Application has forbidden peer dependencies: {string.Join(", ", partnerResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void InfrastructureProjects_ShouldNotReference_OtherModuleInfrastructures()
    {
        var distilleryResult = Types.InAssembly(Assembly.Load("StillOps.Distillery.Infrastructure"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Commerce.Infrastructure",
                "StillOps.Payments.Infrastructure",
                "StillOps.Partner.Infrastructure")
            .GetResult();
        Assert.True(distilleryResult.IsSuccessful,
            $"StillOps.Distillery.Infrastructure has forbidden peer dependencies: {string.Join(", ", distilleryResult.FailingTypeNames ?? [])}");

        var commerceResult = Types.InAssembly(Assembly.Load("StillOps.Commerce.Infrastructure"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Infrastructure",
                "StillOps.Payments.Infrastructure",
                "StillOps.Partner.Infrastructure")
            .GetResult();
        Assert.True(commerceResult.IsSuccessful,
            $"StillOps.Commerce.Infrastructure has forbidden peer dependencies: {string.Join(", ", commerceResult.FailingTypeNames ?? [])}");

        var paymentsResult = Types.InAssembly(Assembly.Load("StillOps.Payments.Infrastructure"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Infrastructure",
                "StillOps.Commerce.Infrastructure",
                "StillOps.Partner.Infrastructure")
            .GetResult();
        Assert.True(paymentsResult.IsSuccessful,
            $"StillOps.Payments.Infrastructure has forbidden peer dependencies: {string.Join(", ", paymentsResult.FailingTypeNames ?? [])}");

        var partnerResult = Types.InAssembly(Assembly.Load("StillOps.Partner.Infrastructure"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "StillOps.Distillery.Infrastructure",
                "StillOps.Commerce.Infrastructure",
                "StillOps.Payments.Infrastructure")
            .GetResult();
        Assert.True(partnerResult.IsSuccessful,
            $"StillOps.Partner.Infrastructure has forbidden peer dependencies: {string.Join(", ", partnerResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void ApiProjects_ShouldNotReference_InfrastructureProjects()
    {
        string[] forbiddenAssemblies =
        [
            "StillOps.Distillery.Infrastructure",
            "StillOps.Commerce.Infrastructure",
            "StillOps.Payments.Infrastructure",
            "StillOps.Partner.Infrastructure"
        ];

        var distilleryResult = Types.InAssembly(Assembly.Load("StillOps.Distillery.Api"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(distilleryResult.IsSuccessful,
            $"StillOps.Distillery.Api has forbidden infrastructure dependencies: {string.Join(", ", distilleryResult.FailingTypeNames ?? [])}");

        var commerceResult = Types.InAssembly(Assembly.Load("StillOps.Commerce.Api"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(commerceResult.IsSuccessful,
            $"StillOps.Commerce.Api has forbidden infrastructure dependencies: {string.Join(", ", commerceResult.FailingTypeNames ?? [])}");

        var paymentsResult = Types.InAssembly(Assembly.Load("StillOps.Payments.Api"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(paymentsResult.IsSuccessful,
            $"StillOps.Payments.Api has forbidden infrastructure dependencies: {string.Join(", ", paymentsResult.FailingTypeNames ?? [])}");

        var partnerResult = Types.InAssembly(Assembly.Load("StillOps.Partner.Api"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();
        Assert.True(partnerResult.IsSuccessful,
            $"StillOps.Partner.Api has forbidden infrastructure dependencies: {string.Join(", ", partnerResult.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void BuildingBlocks_ShouldNotReference_AnyModuleOrEntrypointProject()
    {
        string[] forbiddenAssemblies =
        [
            "StillOps.Distillery.Domain",
            "StillOps.Commerce.Domain",
            "StillOps.Payments.Domain",
            "StillOps.Partner.Domain",
            "StillOps.Distillery.Application",
            "StillOps.Commerce.Application",
            "StillOps.Payments.Application",
            "StillOps.Partner.Application",
            "StillOps.Distillery.Infrastructure",
            "StillOps.Commerce.Infrastructure",
            "StillOps.Payments.Infrastructure",
            "StillOps.Partner.Infrastructure",
            "StillOps.Distillery.Api",
            "StillOps.Commerce.Api",
            "StillOps.Payments.Api",
            "StillOps.Partner.Api",
            "StillOps.Web",
            "StillOps.Ingestion",
            "StillOps.AppHost",
            "StillOps.ServiceDefaults"
        ];

        var result = Types.InAssembly(Assembly.Load("StillOps.BuildingBlocks"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"BuildingBlocks has forbidden dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void IntegrationEvents_ShouldNotReference_AnyModuleProject()
    {
        string[] forbiddenAssemblies =
        [
            "StillOps.Distillery.Domain",
            "StillOps.Commerce.Domain",
            "StillOps.Payments.Domain",
            "StillOps.Partner.Domain",
            "StillOps.Distillery.Application",
            "StillOps.Commerce.Application",
            "StillOps.Payments.Application",
            "StillOps.Partner.Application",
            "StillOps.Distillery.Infrastructure",
            "StillOps.Commerce.Infrastructure",
            "StillOps.Payments.Infrastructure",
            "StillOps.Partner.Infrastructure",
            "StillOps.Distillery.Api",
            "StillOps.Commerce.Api",
            "StillOps.Payments.Api",
            "StillOps.Partner.Api"
        ];

        var result = Types.InAssembly(Assembly.Load("StillOps.Integration.Events"))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblies)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Integration.Events has forbidden module dependencies: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
