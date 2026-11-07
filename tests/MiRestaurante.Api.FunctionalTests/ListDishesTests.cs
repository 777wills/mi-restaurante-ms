using System.Net;
using AwesomeAssertions;
using static MiRestaurante.Api.FunctionalTests.DishApi;

namespace MiRestaurante.Api.FunctionalTests;

public class ListDishesTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Task<HttpResponseMessage> GetAsync(string query = "") =>
        factory.CreateClient().GetAsync(BasePath + query, Token);

    [Fact]
    public async Task List_Should_ReturnOkWithDishesOrderedByName_When_NoFilterIsGiven()
    {
        await InsertAsync(
            factory,
            NewDish("banana"), NewDish("Zeta"), NewDish("ñame"), NewDish("Apple"),
            NewDish("cherry"), NewDish("nube"), NewDish("árbol"));

        var names = await ReadNamesAsync(await GetAsync());

        names.Should().Equal("Apple", "árbol", "banana", "cherry", "nube", "ñame", "Zeta");
    }

    [Theory]
    [InlineData("true", "Disponible")]
    [InlineData("True", "Disponible")]
    [InlineData("TRUE", "Disponible")]
    [InlineData("false", "Agotado")]
    [InlineData("FALSE", "Agotado")]
    public async Task List_Should_ReturnOnlyMatchingDishes_When_AvailableFilterIsGiven(
        string available, string expectedName)
    {
        await InsertAsync(factory, NewDish("Disponible", isAvailable: true), NewDish("Agotado", isAvailable: false));

        var names = await ReadNamesAsync(await GetAsync($"?available={available}"));

        names.Should().Equal(expectedName);
    }

    [Fact]
    public async Task List_Should_ReturnAllActiveDishes_When_AvailableFilterIsEmpty()
    {
        await InsertAsync(factory, NewDish("Disponible", isAvailable: true), NewDish("Agotado", isAvailable: false));

        var names = await ReadNamesAsync(await GetAsync("?available="));

        names.Should().Equal("Agotado", "Disponible");
    }

    [Fact]
    public async Task List_Should_NotIncludeDeletedDishes_When_DishIsDeleted()
    {
        await InsertAsync(factory, NewDish("Activo"), NewDish("Eliminado", deletedAt: DeletedAt));

        var names = await ReadNamesAsync(await GetAsync());

        names.Should().Equal("Activo");
    }

    [Fact]
    public async Task List_Should_ReturnEmptyArray_When_NoActiveDishesExist()
    {
        await InsertAsync(factory, NewDish("Eliminado", deletedAt: DeletedAt));

        var response = await GetAsync();

        (await response.Content.ReadAsStringAsync(Token)).Should().Be("[]");
    }

    [Theory]
    [InlineData("si")]
    [InlineData("1")]
    public async Task List_Should_ReturnBadRequest_When_AvailableFilterIsInvalid(string available)
    {
        var response = await GetAsync($"?available={available}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("si")]
    [InlineData("1")]
    public async Task List_Should_UseProblemJsonContentType_When_AvailableFilterIsInvalid(string available)
    {
        var response = await GetAsync($"?available={available}");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("si")]
    [InlineData("1")]
    public async Task List_Should_ReportSpanishMessageUnderAvailableKey_When_AvailableFilterIsInvalid(string available)
    {
        var problem = await ReadJsonAsync(await GetAsync($"?available={available}"));

        problem.GetProperty("errors").GetProperty("available").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("El valor indicado no es válido.");
    }
}
