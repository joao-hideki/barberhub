using BarberHub.Web.Pagamentos;
using Microsoft.Extensions.Time.Testing;

namespace BarberHub.Tests;

/// <summary>Regras do simulador usado na demonstração (cartões de teste documentados no README).</summary>
public class GatewayPagamentoSimuladoTests
{
    private readonly GatewayPagamentoSimulado _gateway =
        new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero)));

    private Task<RespostaGateway> Cobrar(string numero, string validade = "12/30", string cvv = "123") =>
        _gateway.Cobrar(new Cobranca(99.90m, "teste", new DadosCartao(numero, "JOSE SILVA", validade, cvv)));

    [Fact]
    public async Task Cartao_valido_e_aprovado_com_id_de_transacao()
    {
        var resposta = await Cobrar("4111 1111 1111 1111");

        Assert.True(resposta.Aprovado);
        Assert.StartsWith("SIM-", resposta.TransacaoId);
    }

    [Fact]
    public async Task Cartao_terminado_em_0000_simula_recusa_da_operadora()
    {
        var resposta = await Cobrar("4111 1111 1111 0000");

        Assert.False(resposta.Aprovado);
        Assert.Equal("Cartão recusado pela operadora (simulação).", resposta.Mensagem);
    }

    [Theory]
    [InlineData("4111 1111", "12/30", "123")]            // número curto
    [InlineData("4111 1111 1111 1111", "10/26", "123")]  // vencido (hoje é 11/2026)
    [InlineData("4111 1111 1111 1111", "13/30", "123")]  // mês inválido
    [InlineData("4111 1111 1111 1111", "12/30", "1")]    // CVV inválido
    public async Task Dados_do_cartao_invalidos_sao_recusados(string numero, string validade, string cvv)
    {
        var resposta = await Cobrar(numero, validade, cvv);

        Assert.False(resposta.Aprovado);
        Assert.Equal("Dados do cartão inválidos.", resposta.Mensagem);
    }
}
