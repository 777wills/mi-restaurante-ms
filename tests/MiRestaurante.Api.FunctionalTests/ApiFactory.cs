using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiRestaurante.Api.FunctionalTests.TestControllers;

namespace MiRestaurante.Api.FunctionalTests;

public class ApiFactory(string environment, PostgreSqlFixture database)
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    public CapturingLoggerProvider Logs { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync() => ConnectionString = await database.CreateDatabaseAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorsController).Assembly));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }
}

public sealed class DevelopmentApiFactory(PostgreSqlFixture database) : ApiFactory("Development", database);

public sealed class ProductionApiFactory(PostgreSqlFixture database) : ApiFactory("Production", database);
