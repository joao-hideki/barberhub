using BarberHub.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace BarberHub.Tests.Infra;

/// <summary>
/// Cada teste recebe um banco novo e vazio dentro do container,
/// e um relógio controlado: segunda-feira, 23/11/2026, 09:00 em Brasília.
/// </summary>
[Collection(BancoCollection.Nome)]
public abstract class TesteComBanco : IAsyncLifetime
{
    private readonly string _connectionString;

    protected TesteComBanco(PostgresFixture banco)
    {
        _connectionString = new NpgsqlConnectionStringBuilder(banco.ConnectionString)
        {
            Database = "teste_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
    }

    protected AppDbContext Db { get; private set; } = null!;

    protected FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

    protected AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);

    public async Task InitializeAsync()
    {
        Db = NovoContexto();
        await Db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Db.Database.EnsureDeletedAsync();
        await Db.DisposeAsync();
    }
}
