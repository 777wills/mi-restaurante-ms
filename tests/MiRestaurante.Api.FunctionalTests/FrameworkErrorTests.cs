using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;

namespace MiRestaurante.Api.FunctionalTests;

public class FrameworkErrorTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>
{
    private const string BodyPath = "/api/v1/test-errors/body";
    private const string InvalidFormatMessage = "El cuerpo de la petición no tiene un formato válido.";
    private const string MissingBodyMessage = "Se requiere un cuerpo en la petición.";

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private Task<HttpResponseMessage> PostJsonAsync(string body) =>
        factory.CreateClient().PostAsync(BodyPath, Json(body), TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    private static IEnumerable<string?> ErrorMessages(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject()
            .SelectMany(error => error.Value.EnumerateArray())
            .Select(message => message.GetString())
            .Distinct();

    [Theory]
    [InlineData("/api/v1/no-existe")]
    [InlineData("/otra-ruta")]
    public async Task Get_Should_Return404_When_RouteDoesNotExist(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/v1/no-existe")]
    [InlineData("/otra-ruta")]
    public async Task Get_Should_UseProblemJsonContentType_When_RouteDoesNotExist(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("/api/v1/no-existe")]
    [InlineData("/otra-ruta")]
    public async Task Get_Should_UseResourceNotFoundTitle_When_RouteDoesNotExist(string path)
    {
        var problem = await ReadProblemAsync(
            await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken));

        problem.GetProperty("title").GetString().Should().Be("Recurso no encontrado");
    }

    [Theory]
    [InlineData("/api/v1/no-existe")]
    [InlineData("/otra-ruta")]
    public async Task Get_Should_IncludeTraceId_When_RouteDoesNotExist(string path)
    {
        var problem = await ReadProblemAsync(
            await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken));

        problem.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Should_Return405_When_MethodIsNotAllowed()
    {
        var response = await factory.CreateClient()
            .DeleteAsync("/api/v1/test-errors/not-found", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Delete_Should_UseMethodNotAllowedTitle_When_MethodIsNotAllowed()
    {
        var problem = await ReadProblemAsync(await factory.CreateClient()
            .DeleteAsync("/api/v1/test-errors/not-found", TestContext.Current.CancellationToken));

        problem.GetProperty("title").GetString().Should().Be("Método no permitido");
    }

    [Fact]
    public async Task Post_Should_Return415_When_ContentTypeIsNotSupported()
    {
        var response = await factory.CreateClient().PostAsync(
            BodyPath,
            new StringContent("hola", Encoding.UTF8, "text/plain"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Post_Should_UseUnsupportedContentTypeTitle_When_ContentTypeIsNotSupported()
    {
        var problem = await ReadProblemAsync(await factory.CreateClient().PostAsync(
            BodyPath,
            new StringContent("hola", Encoding.UTF8, "text/plain"),
            TestContext.Current.CancellationToken));

        problem.GetProperty("title").GetString().Should().Be("Tipo de contenido no admitido");
    }

    [Fact]
    public async Task Post_Should_Return400_When_RequiredFieldIsMissing()
    {
        var response = await PostJsonAsync("{\"price\":1}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Should_UseValidationTitle_When_RequiredFieldIsMissing()
    {
        var problem = await ReadProblemAsync(await PostJsonAsync("{\"price\":1}"));

        problem.GetProperty("title").GetString().Should().Be("Uno o más errores de validación");
    }

    [Fact]
    public async Task Post_Should_ReportAttributeMessageByField_When_RequiredFieldIsMissing()
    {
        var problem = await ReadProblemAsync(await PostJsonAsync("{\"price\":1}"));

        problem.GetProperty("errors").GetProperty("Name")[0].GetString().Should().Be("El nombre es obligatorio");
    }

    [Fact]
    public async Task Post_Should_IncludeTraceId_When_RequiredFieldIsMissing()
    {
        var problem = await ReadProblemAsync(await PostJsonAsync("{\"price\":1}"));

        problem.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("{bad")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}")]
    [InlineData("[1]")]
    [InlineData("")]
    [InlineData("null")]
    public async Task Post_Should_Return400_When_BodyCannotBeRead(string body)
    {
        var response = await PostJsonAsync(body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("{bad")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}")]
    [InlineData("[1]")]
    [InlineData("")]
    [InlineData("null")]
    public async Task Post_Should_UseValidationTitle_When_BodyCannotBeRead(string body)
    {
        var problem = await ReadProblemAsync(await PostJsonAsync(body));

        problem.GetProperty("title").GetString().Should().Be("Uno o más errores de validación");
    }

    [Theory]
    [InlineData("{bad")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}")]
    [InlineData("[1]")]
    public async Task Post_Should_ReportGenericFormatMessage_When_BodyIsMalformed(string body)
    {
        var problem = await ReadProblemAsync(await PostJsonAsync(body));

        ErrorMessages(problem).Should().Equal(InvalidFormatMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public async Task Post_Should_ReportMissingBodyMessage_When_BodyIsEmpty(string body)
    {
        var problem = await ReadProblemAsync(await PostJsonAsync(body));

        ErrorMessages(problem).Should().Equal(MissingBodyMessage);
    }

    [Theory]
    [InlineData("{bad", "System.")]
    [InlineData("{bad", "MiRestaurante")]
    [InlineData("{bad", "Path:")]
    [InlineData("{bad", "LineNumber")]
    [InlineData("{bad", "BytePositionInLine")]
    [InlineData("{bad", "is required")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}", "System.")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}", "MiRestaurante")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}", "Path:")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}", "LineNumber")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}", "BytePositionInLine")]
    [InlineData("{\"name\":\"x\",\"price\":\"abc\"}", "is required")]
    [InlineData("[1]", "System.")]
    [InlineData("[1]", "MiRestaurante")]
    [InlineData("[1]", "Path:")]
    [InlineData("[1]", "LineNumber")]
    [InlineData("[1]", "BytePositionInLine")]
    [InlineData("[1]", "is required")]
    [InlineData("", "System.")]
    [InlineData("", "MiRestaurante")]
    [InlineData("", "Path:")]
    [InlineData("", "LineNumber")]
    [InlineData("", "BytePositionInLine")]
    [InlineData("", "is required")]
    [InlineData("null", "System.")]
    [InlineData("null", "MiRestaurante")]
    [InlineData("null", "Path:")]
    [InlineData("null", "LineNumber")]
    [InlineData("null", "BytePositionInLine")]
    [InlineData("null", "is required")]
    public async Task Post_Should_NotExposeInternalData_When_BodyCannotBeRead(string body, string forbidden)
    {
        var text = await (await PostJsonAsync(body)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        text.Should().NotContain(forbidden);
    }

    [Fact]
    public async Task Post_Should_NotUseEnglishRequiredMessage_When_RequiredFieldIsMissing()
    {
        var text = await (await PostJsonAsync("{\"price\":1}")).Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken);

        text.Should().NotContain("is required");
    }
}
