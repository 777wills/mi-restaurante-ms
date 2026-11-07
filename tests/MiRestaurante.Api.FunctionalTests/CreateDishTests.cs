using System.Net;
using AwesomeAssertions;
using static MiRestaurante.Api.FunctionalTests.DishApi;

namespace MiRestaurante.Api.FunctionalTests;

public class CreateDishTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>, IAsyncLifetime
{
    private const string Ten = "aaaaaaaaaa";
    private const string LongName = Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + "a";

    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Task<HttpResponseMessage> PostAsync(string body) =>
        factory.CreateClient().PostAsync(BasePath, Json(body), Token);

    [Fact]
    public async Task Create_Should_ReturnCreated_When_RequestIsValid()
    {
        var response = await PostAsync(ValidBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Should_ReturnLocationOfNewDish_When_RequestIsValid()
    {
        var response = await PostAsync(ValidBody());
        var dish = await ReadDishAsync(response);

        response.Headers.Location!.AbsolutePath.Should().Be($"{BasePath}/{dish.Id}");
    }

    [Fact]
    public async Task Create_Should_ReturnEqualTimestamps_When_RequestIsValid()
    {
        var dish = await ReadDishAsync(await PostAsync(ValidBody()));

        dish.CreatedAt.Should().Be(dish.UpdatedAt);
    }

    [Fact]
    public async Task Create_Should_ReturnTrimmedName_When_NameHasSurroundingSpaces()
    {
        var dish = await ReadDishAsync(await PostAsync(ValidBody("  Ajiaco  ")));

        dish.Name.Should().Be("Ajiaco");
    }

    [Fact]
    public async Task Create_Should_ReturnNullDescription_When_DescriptionIsWhiteSpace()
    {
        var dish = await ReadDishAsync(
            await PostAsync("{\"name\":\"Ajiaco\",\"description\":\" \",\"price\":32000,\"isAvailable\":true}"));

        dish.Description.Should().BeNull();
    }

    [Fact]
    public async Task Create_Should_ReturnCreated_When_PriceHasTrailingZeros()
    {
        var response = await PostAsync("{\"name\":\"Ajiaco\",\"price\":1000.550,\"isAvailable\":true}");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Should_IgnoreUnknownFields_When_BodyHasExtraProperties()
    {
        var response = await PostAsync(
            "{\"name\":\"Ajiaco\",\"price\":32000,\"isAvailable\":true,\"extra\":\"valor\"}");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("{\"name\":\"   \",\"price\":1000,\"isAvailable\":true}",
        "Name", "El nombre es obligatorio.")]
    [InlineData("{\"name\":\"" + LongName + "\",\"price\":1000,\"isAvailable\":true}",
        "Name", "El nombre no puede superar los 100 caracteres.")]
    [InlineData("{\"name\":\"Ajiaco\",\"price\":0,\"isAvailable\":true}",
        "Price", "El precio debe ser mayor que 0 y menor o igual que 99.999.999,99.")]
    [InlineData("{\"name\":\"Ajiaco\",\"price\":1000.555,\"isAvailable\":true}",
        "Price", "El precio admite como máximo 2 decimales.")]
    [InlineData("{\"name\":\"Ajiaco\",\"price\":1000,\"imageUrl\":\"ftp://x/y\",\"isAvailable\":true}",
        "ImageUrl", "La URL de la imagen debe ser una URL absoluta http o https.")]
    [InlineData("{\"name\":\"Ajiaco\",\"price\":1000}",
        "IsAvailable", "La disponibilidad es obligatoria.")]
    [InlineData("{\"name\":\"Ajiaco\",\"isAvailable\":true}",
        "Price", "El precio es obligatorio.")]
    public async Task Create_Should_ReturnBadRequestWithSpanishMessage_When_FieldIsInvalid(
        string body, string key, string message)
    {
        var problem = await ReadJsonAsync(await PostAsync(body));

        problem.GetProperty("errors").GetProperty(key).EnumerateArray().Select(e => e.GetString())
            .Should().Contain(message);
    }

    [Fact]
    public async Task Create_Should_NotCreateDish_When_RequestIsInvalid()
    {
        await PostAsync("{\"name\":\"Ajiaco\",\"price\":0,\"isAvailable\":true}");

        var response = await factory.CreateClient().GetAsync(BasePath, Token);

        (await response.Content.ReadAsStringAsync(Token)).Should().Be("[]");
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_BodyIsMissing()
    {
        var response = await PostAsync(string.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Should_UseProblemJsonContentType_When_BodyIsMissing()
    {
        var response = await PostAsync(string.Empty);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_JsonIsMalformed()
    {
        var response = await PostAsync("{\"name\":");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Should_UseProblemJsonContentType_When_JsonIsMalformed()
    {
        var response = await PostAsync("{\"name\":");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Create_Should_ReturnUnprocessableEntity_When_NameIsInUse()
    {
        await PostAsync(ValidBody("Ajiaco"));

        var response = await PostAsync(ValidBody(" AJIACO "));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_Should_UseSpanishDetail_When_NameIsInUse()
    {
        await PostAsync(ValidBody("Ajiaco"));

        var problem = await ReadJsonAsync(await PostAsync(ValidBody(" AJIACO ")));

        problem.GetProperty("detail").GetString().Should().Be("Ya existe un plato con el nombre «AJIACO».");
    }

    [Fact]
    public async Task Create_Should_AcceptOnlyOneRequest_When_TwentyRequestsUseSameNameConcurrently()
    {
        var client = factory.CreateClient();
        var names = Enumerable.Repeat(new[] { "ajiaco", "AJIACO", "Ajiaco", "aJIACO" }, 5).SelectMany(n => n);

        var responses = await Task.WhenAll(names.Select(name => client.PostAsync(BasePath, Json(ValidBody(name)), Token)));

        responses.Select(r => (int)r.StatusCode).Order()
            .Should().Equal(new[] { 201 }.Concat(Enumerable.Repeat(422, 19)));
    }
}
