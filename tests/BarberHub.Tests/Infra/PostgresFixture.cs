using Testcontainers.PostgreSql;

namespace BarberHub.Tests.Infra;

/// <summary>Sobe um PostgreSQL descartável no Docker uma única vez para toda a suíte.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Nome)]
public class BancoCollection : ICollectionFixture<PostgresFixture>
{
    public const string Nome = "banco";
}
