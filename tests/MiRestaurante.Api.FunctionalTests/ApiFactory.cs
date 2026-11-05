using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiRestaurante.Api.FunctionalTests.TestControllers;
using Npgsql;
using Respawn;

namespace MiRestaurante.Api.FunctionalTests;

public class ApiFactory(string environment, PostgreSqlFixture database)
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private NpgsqlConnection? _connection;
    private Respawner? _respawner;

    public CapturingLoggerProvider Logs { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync() => ConnectionString = await database.CreateDatabaseAsync();

    public async Task ResetDatabaseAsync()
    {
        // Starting the host applies the Development migrations before Respawn inspects the tables.
        _ = Services;

        if (_respawner is null)
        {
            _connection = new NpgsqlConnection(ConnectionString);
            await _connection.OpenAsync();
            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = ["__EFMigrationsHistory"],
            });
        }

        await _respawner.ResetAsync(_connection!);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }

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
