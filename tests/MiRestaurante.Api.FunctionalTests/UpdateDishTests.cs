using System.Net;
using AwesomeAssertions;
using static MiRestaurante.Api.FunctionalTests.DishApi;

namespace MiRestaurante.Api.FunctionalTests;

public class UpdateDishTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>, IAsyncLifetime
{
    // Added to the id of a deleted dish to obtain an id that does not exist.
    private const int MissingIdOffset = 1000;
    private const string InvalidPriceBody = "{\"name\":\"Ajiaco\",\"price\":0,\"isAvailable\":true}";

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // Reads the dish back with GET so the timestamps have the precision stored in the database.
    private async Task<DishResponse> CreateAsync(string name)
    {
        var client = factory.CreateClient();
        var created = await ReadDishAsync(await client.PostAsync(BasePath, Json(ValidBody(name)), Token));
        return await ReadDishAsync(await client.GetAsync($"{BasePath}/{created.Id}", Token));
    }

    private Task<HttpResponseMessage> PutAsync(object id, string body) =>
        factory.CreateClient().PutAsync($"{BasePath}/{id}", Json(body), Token);

    [Fact]
    public async Task Update_Should_ReturnOkWithUpdatedDish_When_RequestIsValid()
    {
        var original = await CreateAsync("Ajiaco");

        var response = await PutAsync(
            original.Id,
            "{\"name\":\"Sancocho\",\"description\":\"Caldo de hueso.\",\"price\":28000.5,"
            + "\"imageUrl\":\"https://example.com/sancocho.jpg\",\"isAvailable\":false}");

        (await ReadDishAsync(response)).Should().BeEquivalentTo(new
        {
            original.Id,
            Name = "Sancocho",
            Description = "Caldo de hueso.",
            Price = 28000.5m,
            ImageUrl = "https://example.com/sancocho.jpg",
            IsAvailable = false,
        });
    }

    [Fact]
    public async Task Update_Should_KeepCreatedAt_When_RequestIsValid()
    {
        var original = await CreateAsync("Ajiaco");

        var updated = await ReadDishAsync(await PutAsync(original.Id, ValidBody("Sancocho")));

        updated.CreatedAt.Should().Be(original.CreatedAt);
    }

    [Fact]
    public async Task Update_Should_NotMoveUpdatedAtBackwards_When_RequestIsValid()
    {
        var original = await CreateAsync("Ajiaco");

        var updated = await ReadDishAsync(await PutAsync(original.Id, ValidBody("Sancocho")));

        updated.UpdatedAt.Should().BeOnOrAfter(original.UpdatedAt);
    }

    [Fact]
    public async Task Update_Should_ReturnBadRequest_When_RequestIsInvalid()
    {
        var original = await CreateAsync("Ajiaco");

        var response = await PutAsync(original.Id, InvalidPriceBody);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Should_NotModifyDish_When_RequestIsInvalid()
    {
        var original = await CreateAsync("Ajiaco");
        await PutAsync(original.Id, InvalidPriceBody);

        var current = await ReadDishAsync(await factory.CreateClient().GetAsync($"{BasePath}/{original.Id}", Token));

        current.Should().BeEquivalentTo(original);
    }

    [Fact]
    public async Task Update_Should_ReturnBadRequest_When_DishDoesNotExistAndRequestIsInvalid()
    {
        var response = await PutAsync(999, InvalidPriceBody);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Should_ReturnBadRequest_When_BodyIsMissing()
    {
        var original = await CreateAsync("Ajiaco");

        var response = await PutAsync(original.Id, string.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Should_ReturnBadRequest_When_JsonIsMalformed()
    {
        var original = await CreateAsync("Ajiaco");

        var response = await PutAsync(original.Id, "{\"name\":");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{\"name\":")]
    public async Task Update_Should_UseProblemJsonContentType_When_BodyIsMissingOrJsonIsMalformed(string body)
    {
        var original = await CreateAsync("Ajiaco");

        var response = await PutAsync(original.Id, body);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData(MissingIdOffset)]
    [InlineData(0)]
    public async Task Update_Should_ReturnNotFound_When_DishDoesNotExistOrIsDeleted(int offset)
    {
        var deleted = NewDish("Eliminado", deletedAt: DeletedAt);
        await InsertAsync(factory, deleted);

        var response = await PutAsync(deleted.Id + offset, ValidBody("Sancocho"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(MissingIdOffset)]
    [InlineData(0)]
    public async Task Update_Should_UseSpanishDetail_When_DishDoesNotExistOrIsDeleted(int offset)
    {
        var deleted = NewDish("Eliminado", deletedAt: DeletedAt);
        await InsertAsync(factory, deleted);
        var id = deleted.Id + offset;

        var problem = await ReadJsonAsync(await PutAsync(id, ValidBody("Sancocho")));

        problem.GetProperty("detail").GetString().Should().Be($"No existe el plato {id}.");
    }

    [Fact]
    public async Task Update_Should_ReturnNotFound_When_DishDoesNotExistAndNameIsInUse()
    {
        var ajiaco = await CreateAsync("Ajiaco");

        var response = await PutAsync(ajiaco.Id + MissingIdOffset, ValidBody("Ajiaco"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Update_Should_ReturnNotFound_When_IdIsNotAPositiveInteger(string id)
    {
        var response = await PutAsync(id, ValidBody("Sancocho"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Should_ReturnUnprocessableEntity_When_NameBelongsToOtherActiveDish()
    {
        await CreateAsync("Ajiaco");
        var sancocho = await CreateAsync("Sancocho");

        var response = await PutAsync(sancocho.Id, ValidBody("ajiaco"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Update_Should_UseSpanishDetail_When_NameBelongsToOtherActiveDish()
    {
        await CreateAsync("Ajiaco");
        var sancocho = await CreateAsync("Sancocho");

        var problem = await ReadJsonAsync(await PutAsync(sancocho.Id, ValidBody("ajiaco")));

        problem.GetProperty("detail").GetString().Should().Be("Ya existe un plato con el nombre «ajiaco».");
    }

    [Fact]
    public async Task Update_Should_ExcludeDishFromAvailableList_When_DishIsMarkedUnavailable()
    {
        var ajiaco = await CreateAsync("Ajiaco");
        await PutAsync(ajiaco.Id, ValidBody("Ajiaco", isAvailable: false));

        var names = await ReadNamesAsync(await factory.CreateClient().GetAsync($"{BasePath}?available=true", Token));

        names.Should().BeEmpty();
    }
}
