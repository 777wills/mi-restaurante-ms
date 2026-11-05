using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using static MiRestaurante.Api.FunctionalTests.DishApi;

namespace MiRestaurante.Api.FunctionalTests;

public class GetDishTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>, IAsyncLifetime
{
    // Added to the id of a deleted dish to obtain an id that does not exist.
    private const int MissingIdOffset = 1000;

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Task<HttpResponseMessage> GetAsync(object id) =>
        factory.CreateClient().GetAsync($"{BasePath}/{id}", Token);

    [Fact]
    public async Task GetById_Should_ReturnOkWithDish_When_DishIsActive()
    {
        var dish = NewDish("Ajiaco");
        await InsertAsync(factory, dish);

        var result = await ReadDishAsync(await GetAsync(dish.Id));

        result.Should().BeEquivalentTo(new DishResponse(
            dish.Id,
            "Ajiaco",
            "Descripción del plato",
            25000.00m,
            "https://example.com/plato.jpg",
            true,
            CreatedAt,
            CreatedAt));
    }

    [Fact]
    public async Task GetById_Should_ReturnExactlyEightProperties_When_DishIsActive()
    {
        var dish = NewDish("Ajiaco");
        await InsertAsync(factory, dish);

        var json = await ReadJsonAsync(await GetAsync(dish.Id));

        json.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "id", "name", "description", "price", "imageUrl", "isAvailable", "createdAt", "updatedAt");
    }

    [Fact]
    public async Task GetById_Should_ReturnNullOptionalFields_When_DishHasNoDescriptionOrImage()
    {
        var dish = NewDish("Ajiaco", description: null, imageUrl: null);
        await InsertAsync(factory, dish);

        var json = await ReadJsonAsync(await GetAsync(dish.Id));

        new[] { json.GetProperty("description").ValueKind, json.GetProperty("imageUrl").ValueKind }
            .Should().AllBeEquivalentTo(JsonValueKind.Null);
    }

    [Theory]
    [InlineData(MissingIdOffset)]
    [InlineData(0)]
    public async Task GetById_Should_ReturnNotFound_When_DishDoesNotExistOrIsDeleted(int offset)
    {
        var deleted = NewDish("Eliminado", deletedAt: DeletedAt);
        await InsertAsync(factory, deleted);

        var response = await GetAsync(deleted.Id + offset);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(MissingIdOffset)]
    [InlineData(0)]
    public async Task GetById_Should_UseSpanishDetail_When_DishDoesNotExistOrIsDeleted(int offset)
    {
        var deleted = NewDish("Eliminado", deletedAt: DeletedAt);
        await InsertAsync(factory, deleted);
        var id = deleted.Id + offset;

        var problem = await ReadJsonAsync(await GetAsync(id));

        problem.GetProperty("detail").GetString().Should().Be($"No existe el plato {id}.");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("99999999999")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task GetById_Should_ReturnNotFound_When_IdIsNotAPositiveInteger(string id)
    {
        var response = await GetAsync(id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("99999999999")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task GetById_Should_UseProblemJsonContentType_When_IdIsNotAPositiveInteger(string id)
    {
        var response = await GetAsync(id);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
