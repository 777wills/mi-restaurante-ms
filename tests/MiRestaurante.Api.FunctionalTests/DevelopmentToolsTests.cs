using System.Net;
using AwesomeAssertions;

namespace MiRestaurante.Api.FunctionalTests;

public class DevelopmentToolsTests(DevelopmentApiFactory factory) : IClassFixture<DevelopmentApiFactory>
{
    private const string PreflightPath = "/api/v1/test-errors/body";

    private Task<HttpResponseMessage> SendPreflightAsync() =>
        factory.CreateClient().SendAsync(PreflightRequest.Create(PreflightPath), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Get_Should_Return200_When_ScalarIsRequested()
    {
        var response = await factory.CreateClient().GetAsync("/scalar", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Should_ReferenceOpenApiDocument_When_ScalarIsRequested()
    {
        var response = await factory.CreateClient().GetAsync("/scalar", TestContext.Current.CancellationToken);

        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("openapi/v1.json");
    }

    [Fact]
    public async Task Get_Should_Return200_When_OpenApiDocumentIsRequested()
    {
        var response = await factory.CreateClient()
            .GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Options_Should_AllowAnyOrigin_When_PreflightIsSent()
    {
        var response = await SendPreflightAsync();

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal("*");
    }

    [Fact]
    public async Task Options_Should_AllowRequestedMethod_When_PreflightIsSent()
    {
        var response = await SendPreflightAsync();

        response.Headers.GetValues("Access-Control-Allow-Methods").Should().Equal("POST");
    }

    [Fact]
    public async Task Options_Should_AllowRequestedHeader_When_PreflightIsSent()
    {
        var response = await SendPreflightAsync();

        response.Headers.GetValues("Access-Control-Allow-Headers").Should().Equal("x-foo");
    }

    [Fact]
    public async Task Options_Should_NotAllowCredentials_When_PreflightIsSent()
    {
        var response = await SendPreflightAsync();

        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }
}
