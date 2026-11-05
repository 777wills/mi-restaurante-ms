using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiRestaurante.Application;

namespace MiRestaurante.Infrastructure.IntegrationTests;

public class ApplicationDbContextTests(PostgreSqlFixture database)
{
    private async Task<ServiceProvider> BuildProviderAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = await database.CreateDatabaseAsync(),
            })
            .Build();
        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddInfrastructure()
            .BuildServiceProvider();
    }

    [Fact]
    public async Task MigrateAsync_Should_RegisterAllMigrations_When_DatabaseIsNew()
    {
        await using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken))
            .Should().Equal(db.Database.GetMigrations());
    }

    [Fact]
    public async Task HasPendingModelChanges_Should_BeFalse_When_DatabaseIsMigrated()
    {
        await using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public async Task Resolve_Should_ReturnApplicationDbContext_When_ApplicationDbContextInterfaceIsRequested()
    {
        await using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IApplicationDbContext>().Should().BeOfType<ApplicationDbContext>();
    }

    [Fact]
    public async Task Resolve_Should_ReturnSameInstance_When_ResolvedTwiceInSameScope()
    {
        await using var provider = await BuildProviderAsync();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IApplicationDbContext>()
            .Should().BeSameAs(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
