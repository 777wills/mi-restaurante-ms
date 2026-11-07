using System.Net;
using AwesomeAssertions;

namespace MiRestaurante.Api.FunctionalTests;

public class ProductionEnvironmentTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    private const string PreflightPath = "/api/v1/test-errors/body";

    [Theory]
    [InlineData("/scalar")]
    [InlineData("/openapi/v1.json")]
    public async Task Get_Should_Return404_When_DevelopmentToolIsRequested(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/scalar")]
    [InlineData("/openapi/v1.json")]
    public async Task Get_Should_UseProblemJsonContentType_When_DevelopmentToolIsRequested(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Get_Should_OmitCorsHeaders_When_RequestHasOrigin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test-errors/not-found");
        request.Headers.TryAddWithoutValidation("Origin", "http://otro-origen.test");

        var response = await factory.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.Select(header => header.Key).Should().NotContain(key => key.StartsWith("Access-Control"));
    }

    [Fact]
    public async Task Options_Should_OmitCorsHeaders_When_PreflightIsSent()
    {
        var response = await factory.CreateClient()
            .SendAsync(PreflightRequest.Create(PreflightPath), TestContext.Current.CancellationToken);

        response.Headers.Select(header => header.Key).Should().NotContain(key => key.StartsWith("Access-Control"));
    }
}
