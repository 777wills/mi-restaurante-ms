using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace MiRestaurante.Api.FunctionalTests;

public class TimeProviderTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>
{
    [Fact]
    public void Resolve_Should_ReturnSystemTimeProvider_When_ServiceIsRequested()
    {
        var timeProvider = factory.Services.GetRequiredService<TimeProvider>();

        timeProvider.Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void Resolve_Should_ReturnSameInstance_When_ServiceIsRequestedTwice()
    {
        var first = factory.Services.GetRequiredService<TimeProvider>();
        var second = factory.Services.GetRequiredService<TimeProvider>();

        first.Should().BeSameAs(second);
    }
}
