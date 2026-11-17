using BarberHub.Tests.Infra;
using BarberHub.Web.Domain;
using BarberHub.Web.Pagamentos;
using BarberHub.Web.Services;

namespace BarberHub.Tests;

/// <summary>Gateway externo é a única dependência substituída nos testes.</summary>
public class GatewayFalso : IGatewayPagamento
{
    public bool Aprovar { get; set; } = true;
    public List<Cobranca> Cobrancas { get; } = [];

    public Task<RespostaGateway> Cobrar(Cobranca cobranca)
    {
        Cobrancas.Add(cobranca);
        return Task.FromResult(Aprovar
            ? new RespostaGateway(true, "tx-123", "Pagamento aprovado.")
            : new RespostaGateway(false, null, "Cartão sem limite."));
    }
}

public class AssinaturaServiceTests(PostgresFixture banco) : TesteComBanco(banco)
{
    private readonly GatewayFalso _gateway = new();
    private static readonly DadosCartao Cartao = new("4111 1111 1111 1111", "JOSE SILVA", "12/30", "123");

    private AssinaturaService Assinatura => new(Db, _gateway, Relogio);

    [Fact]
    public async Task Pagamento_aprovado_durante_periodo_gratis_soma_um_mes_sem_perder_os_dias_gratis()
    {
        var barbearia = await CriarBarbearia(Plano.Basico); // grátis até 07/12

        var resultado = await Assinatura.Checkout(barbearia.Id, Plano.Pro, Cartao);

        Assert.True(resultado.Sucesso, resultado.Erro);
        Assert.Equal(Plano.Pro, resultado.Valor!.Plano);
        Assert.False(resultado.Valor.EmPeriodoGratis);
        Assert.Equal(new DateOnly(2027, 1, 7), resultado.Valor.AssinaturaValidaAte);
        Assert.Equal(99.90m, Assert.Single(_gateway.Cobrancas).Valor);
    }
}
