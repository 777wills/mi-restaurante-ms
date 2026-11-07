using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MiRestaurante.Api.FunctionalTests;

public class ExceptionHandlingTests(DevelopmentApiFactory development, ProductionApiFactory production)
    : IClassFixture<DevelopmentApiFactory>, IClassFixture<ProductionApiFactory>
{
    private const string BasePath = "/api/v1/test-errors/";

    private static Task<HttpResponseMessage> GetAsync(ApiFactory factory, string action) =>
        factory.CreateClient().GetAsync(BasePath + action, TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task Get_Should_Return404_When_NotFoundExceptionIsThrown()
    {
        var response = await GetAsync(development, "not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Should_UseResourceNotFoundTitle_When_NotFoundExceptionIsThrown()
    {
        var problem = await ReadProblemAsync(await GetAsync(development, "not-found"));

        problem.GetProperty("title").GetString().Should().Be("Recurso no encontrado");
    }

    [Fact]
    public async Task Get_Should_UseExceptionMessageAsDetail_When_NotFoundExceptionIsThrown()
    {
        var problem = await ReadProblemAsync(await GetAsync(development, "not-found"));

        problem.GetProperty("detail").GetString().Should().Be("No existe el plato 7");
    }

    [Fact]
    public async Task Get_Should_Return422_When_BusinessRuleExceptionIsThrown()
    {
        var response = await GetAsync(development, "business-rule");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Get_Should_UseBusinessRuleTitle_When_BusinessRuleExceptionIsThrown()
    {
        var problem = await ReadProblemAsync(await GetAsync(development, "business-rule"));

        problem.GetProperty("title").GetString().Should().Be("Regla de negocio incumplida");
    }

    [Fact]
    public async Task Get_Should_UseExceptionMessageAsDetail_When_BusinessRuleExceptionIsThrown()
    {
        var problem = await ReadProblemAsync(await GetAsync(development, "business-rule"));

        problem.GetProperty("detail").GetString().Should().Be("Plato no disponible");
    }

    [Fact]
    public async Task Get_Should_Return404_When_DerivedNotFoundExceptionIsThrown()
    {
        var response = await GetAsync(development, "derived-not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Should_Return422_When_DerivedBusinessRuleExceptionIsThrown()
    {
        var response = await GetAsync(development, "derived-business-rule");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Get_Should_Return404_When_NotFoundExceptionHasEmptyMessage()
    {
        var response = await GetAsync(development, "not-found-empty");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Should_UseResourceNotFoundTitle_When_NotFoundExceptionHasEmptyMessage()
    {
        var problem = await ReadProblemAsync(await GetAsync(development, "not-found-empty"));

        problem.GetProperty("title").GetString().Should().Be("Recurso no encontrado");
    }

    [Fact]
    public async Task Get_Should_NotExposeExceptionType_When_NotFoundExceptionHasEmptyMessage()
    {
        var body = await ReadBodyAsync(await GetAsync(development, "not-found-empty"));

        body.Should().NotContain("NotFoundException");
    }

    [Fact]
    public async Task Get_Should_Return500_When_UnexpectedExceptionIsThrown()
    {
        var response = await GetAsync(production, "unexpected");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Get_Should_UseInternalErrorTitle_When_UnexpectedExceptionIsThrown()
    {
        var problem = await ReadProblemAsync(await GetAsync(production, "unexpected"));

        problem.GetProperty("title").GetString().Should().Be("Error interno del servidor");
    }

    [Theory]
    [InlineData("secreto-interno")]
    [InlineData("InvalidOperationException")]
    [InlineData("   at ")]
    [InlineData("\"detail\"")]
    public async Task Get_Should_NotExposeInternalData_When_UnexpectedExceptionIsThrownInDevelopment(string forbidden)
    {
        var body = await ReadBodyAsync(await GetAsync(development, "unexpected"));

        body.Should().NotContain(forbidden);
    }

    [Theory]
    [InlineData("secreto-interno")]
    [InlineData("InvalidOperationException")]
    [InlineData("   at ")]
    [InlineData("\"detail\"")]
    public async Task Get_Should_NotExposeInternalData_When_UnexpectedExceptionIsThrownInProduction(string forbidden)
    {
        var body = await ReadBodyAsync(await GetAsync(production, "unexpected"));

        body.Should().NotContain(forbidden);
    }

    [Fact]
    public async Task Get_Should_ReturnOwnStatusCode_When_BadHttpRequestExceptionIsThrown()
    {
        var response = await GetAsync(development, "bad-request-413");

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Get_Should_UseInvalidRequestTitle_When_BadHttpRequestExceptionIsThrown()
    {
        var problem = await ReadProblemAsync(await GetAsync(development, "bad-request-413"));

        problem.GetProperty("title").GetString().Should().Be("Solicitud no válida");
    }

    [Fact]
    public async Task Get_Should_OmitDetail_When_BadHttpRequestExceptionIsThrown()
    {
        var problem = await ReadProblemAsync(await GetAsync(development, "bad-request-413"));

        problem.TryGetProperty("detail", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("not-found")]
    [InlineData("business-rule")]
    [InlineData("unexpected")]
    [InlineData("bad-request-413")]
    public async Task Get_Should_IncludeTraceId_When_ExceptionIsHandled(string action)
    {
        var problem = await ReadProblemAsync(await GetAsync(development, action));

        problem.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("not-found")]
    [InlineData("business-rule")]
    [InlineData("unexpected")]
    [InlineData("bad-request-413")]
    public async Task Get_Should_OmitInstance_When_ExceptionIsHandled(string action)
    {
        var problem = await ReadProblemAsync(await GetAsync(development, action));

        problem.TryGetProperty("instance", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("not-found")]
    [InlineData("business-rule")]
    [InlineData("unexpected")]
    [InlineData("bad-request-413")]
    public async Task Get_Should_UseProblemJsonContentType_When_ExceptionIsHandled(string action)
    {
        var response = await GetAsync(development, action);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Get_Should_LogErrorWithException_When_UnexpectedExceptionIsThrown()
    {
        await GetAsync(development, "unexpected");

        development.Logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error && entry.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task Get_Should_LogInformationWithoutException_When_NotFoundExceptionIsThrown()
    {
        await GetAsync(development, "not-found");

        development.Logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Information
            && entry.Exception == null
            && entry.Message.Contains("No existe el plato 7"));
    }

    [Fact]
    public async Task Get_Should_LogInformationWithoutException_When_BusinessRuleExceptionIsThrown()
    {
        await GetAsync(development, "business-rule");

        development.Logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Information
            && entry.Exception == null
            && entry.Message.Contains("Plato no disponible"));
    }

    [Fact]
    public async Task Get_Should_LogInformationWithoutException_When_BadHttpRequestExceptionIsThrown()
    {
        await GetAsync(development, "bad-request-413");

        development.Logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Information
            && entry.Exception == null
            && entry.Message.Contains("Cuerpo demasiado grande"));
    }

    [Fact]
    public void Services_Should_ContainSingleExceptionHandler_When_ApplicationStarts()
    {
        development.Services.GetServices<IExceptionHandler>().Should().HaveCount(1);
    }
}
