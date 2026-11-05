using AwesomeAssertions;
using static MiRestaurante.Api.FunctionalTests.DishApi;

namespace MiRestaurante.Api.FunctionalTests;

// Read-only: uses its own factory and never resets the database, so the seed data stays intact.
public class DishSeedApiTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>
{
    [Fact]
    public async Task List_Should_ReturnFiveSeedDishes_When_DatabaseIsFreshlyMigrated()
    {
        var names = await ReadNamesAsync(await factory.CreateClient().GetAsync(BasePath, Token));

        names.Should().Equal("Ajiaco santafereño", "Arepa de huevo", "Bandeja paisa", "Lechona", "Sancocho de hueso");
    }

    [Fact]
    public async Task List_Should_ReturnFourDishes_When_FilteringAvailableOnFreshDatabase()
    {
        var names = await ReadNamesAsync(await factory.CreateClient().GetAsync($"{BasePath}?available=true", Token));

        names.Should().Equal("Ajiaco santafereño", "Arepa de huevo", "Bandeja paisa", "Sancocho de hueso");
    }
}
