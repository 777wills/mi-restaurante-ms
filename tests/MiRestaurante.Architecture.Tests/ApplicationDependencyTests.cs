using AwesomeAssertions;

namespace MiRestaurante.Architecture.Tests;

public class ApplicationDependencyTests
{
    private static readonly string[] AllowedAssemblies =
    [
        Layers.Domain,
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions.Logging.Abstractions",
    ];

    [Fact]
    public void Application_Should_ReferenceOnlyAllowedAssemblies_When_Compiled()
    {
        IEnumerable<string> disallowedReferences = Layers.Load(Layers.Application)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !Layers.IsFrameworkAssembly(name))
            .Except(AllowedAssemblies, StringComparer.Ordinal);

        // HaveCount(0) instead of BeEmpty() so the failure message lists every disallowed assembly.
        disallowedReferences.Should().HaveCount(0,
            "the application layer may only depend on the domain, the EF Core core package and logging abstractions");
    }
}
