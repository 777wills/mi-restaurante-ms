using AwesomeAssertions;
using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace MiRestaurante.Architecture.Tests;

public class LayerDependencyTests
{
    [Theory]
    [InlineData(Layers.Domain, Layers.Application)]
    [InlineData(Layers.Domain, Layers.Infrastructure)]
    [InlineData(Layers.Domain, Layers.Api)]
    [InlineData(Layers.Application, Layers.Infrastructure)]
    [InlineData(Layers.Application, Layers.Api)]
    [InlineData(Layers.Infrastructure, Layers.Api)]
    public void Layer_Should_NotDependOnForbiddenLayer_When_DependencyRuleIsApplied(string layer, string forbiddenLayer)
    {
        TestResult result = Types.InAssembly(Layers.Load(layer))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenLayer)
            .GetResult();

        result.FailingTypes.Should().BeNullOrEmpty(
            "{0} must not depend on {1}", layer, forbiddenLayer);
    }

    [Fact]
    public void Controllers_Should_NotDependOnInfrastructure_When_HandlingRequests()
    {
        TestResult result = Types.InAssembly(Layers.Load(Layers.Api))
            .That()
            .ResideInNamespace(Layers.Controllers)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.Infrastructure)
            .GetResult();

        result.FailingTypes.Should().BeNullOrEmpty(
            "controllers must use application services instead of infrastructure");
    }

    [Fact]
    public void Domain_Should_ReferenceOnlyFrameworkAssemblies_When_Compiled()
    {
        IEnumerable<string> nonFrameworkReferences = Layers.Load(Layers.Domain)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !Layers.IsFrameworkAssembly(name));

        nonFrameworkReferences.Should().BeEmpty(
            "the domain must not depend on NuGet packages or other projects");
    }
}
