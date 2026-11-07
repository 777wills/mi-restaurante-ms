using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiRestaurante.Infrastructure;

namespace MiRestaurante.Api.FunctionalTests;

public class PersistenceTests(DevelopmentApiFactory development, ProductionApiFactory production)
    : IClassFixture<DevelopmentApiFactory>, IClassFixture<ProductionApiFactory>
{
    private const string UnreachableConnectionString = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x";

    [Fact]
    public async Task Startup_Should_ApplyAllMigrations_When_EnvironmentIsDevelopment()
    {
        await using var scope = development.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;

        (await database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken))
            .Should().Equal(database.GetMigrations());
    }

    [Fact]
    public async Task Startup_Should_NotApplyMigrations_When_EnvironmentIsProduction()
    {
        await using var scope = production.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;

        (await database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public void Startup_Should_Throw_When_DevelopmentDatabaseIsUnreachable()
    {
        using var factory = development.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", UnreachableConnectionString));

        var act = () => factory.CreateClient();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Startup_Should_LogError_When_DevelopmentDatabaseIsUnreachable()
    {
        using var factory = development.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", UnreachableConnectionString));

        _ = Record.Exception(() => factory.CreateClient());

        development.Logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error && entry.Category == "Microsoft.EntityFrameworkCore.Database.Connection");
    }

    [Fact]
    public async Task Health_Should_ReturnOk_When_DatabaseIsReachable()
    {
        var response = await development.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Should_ReturnHealthyBody_When_DatabaseIsReachable()
    {
        var response = await development.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_Should_ReturnTextPlain_When_DatabaseIsReachable()
    {
        var response = await development.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.Content.Headers.ContentType.Should().Match<MediaTypeHeaderValue>(header => header.MediaType == "text/plain");
    }

    [Fact]
    public async Task Health_Should_ReturnOk_When_EnvironmentIsProduction()
    {
        var response = await production.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Should_ReturnServiceUnavailable_When_ConnectionStringIsMissing()
    {
        using var factory = production.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", string.Empty));

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Health_Should_ReturnServiceUnavailable_When_DatabaseIsUnreachable()
    {
        using var factory = production.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", UnreachableConnectionString));

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Health_Should_ReturnOnlyUnhealthyBody_When_DatabaseIsUnreachable()
    {
        using var factory = production.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", UnreachableConnectionString));

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Unhealthy");
    }

    [Fact]
    public async Task Health_Should_ReturnTextPlain_When_DatabaseIsUnreachable()
    {
        using var factory = production.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", UnreachableConnectionString));

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.Content.Headers.ContentType.Should().Match<MediaTypeHeaderValue>(header => header.MediaType == "text/plain");
    }
}
