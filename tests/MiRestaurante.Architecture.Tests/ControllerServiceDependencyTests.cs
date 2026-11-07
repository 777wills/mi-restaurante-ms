using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace MiRestaurante.Architecture.Tests;

public class ControllerServiceDependencyTests
{
    private static string[] ConcreteServiceNames() =>
        Layers.Load(Layers.Application).GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Service", StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .ToArray();

    [Fact]
    public void Controllers_Should_NotDependOnConcreteServices_When_HandlingRequests()
    {
        TestResult result = Types.InAssembly(Layers.Load(Layers.Api))
            .That()
            .Inherit(typeof(ControllerBase))
            .ShouldNot()
            .HaveDependencyOnAny(ConcreteServiceNames())
            .GetResult();

        result.FailingTypes.Should().BeNullOrEmpty(
            "controllers must depend only on application service interfaces");
    }

    [Fact]
    public void Controllers_Should_NotDependOnDomain_When_HandlingRequests()
    {
        TestResult result = Types.InAssembly(Layers.Load(Layers.Api))
            .That()
            .Inherit(typeof(ControllerBase))
            .ShouldNot()
            .HaveDependencyOnAny(Layers.Domain)
            .GetResult();

        result.FailingTypes.Should().BeNullOrEmpty(
            "controllers must receive DTOs from application services, never domain entities");
    }
}
