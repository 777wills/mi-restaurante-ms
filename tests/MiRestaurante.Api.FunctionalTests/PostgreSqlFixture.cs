using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(MiRestaurante.Api.FunctionalTests.PostgreSqlFixture))]

namespace MiRestaurante.Api.FunctionalTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync());

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"test_{Guid.NewGuid():N}";
        await _container.ExecScriptAsync($"CREATE DATABASE {name}");
        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name }.ConnectionString;
    }
}
