using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Services;
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

    private int _contador;

    /// <summary>Cria uma barbearia pelo fluxo real de cadastro.</summary>
    protected async Task<Barbearia> CriarBarbearia(Plano plano = Plano.Pro)
    {
        var n = ++_contador;
        var resultado = await new CadastroService(Db, Relogio).CadastrarBarbearia(
            new NovaBarbearia($"Barbearia {n}", $"dono{n}@teste.com", "segredo123", plano));
        return resultado.Valor!;
    }

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
