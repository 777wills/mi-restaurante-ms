using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace MiRestaurante.Architecture.Tests;

public class ControllerDataAccessTests
{
    [Fact]
    public void Controllers_Should_NotDependOnApplicationDbContext_When_HandlingRequests()
    {
        TestResult result = Types.InAssembly(Layers.Load(Layers.Api))
            .That()
            .Inherit(typeof(ControllerBase))
            .ShouldNot()
            .HaveDependencyOnAny($"{Layers.Application}.IApplicationDbContext")
            .GetResult();

        result.FailingTypes.Should().BeNullOrEmpty(
            "controllers must use application services instead of IApplicationDbContext");
    }
}
