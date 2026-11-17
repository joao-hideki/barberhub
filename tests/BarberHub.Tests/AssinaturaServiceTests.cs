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

    [Fact]
    public async Task Assinatura_vencida_renova_a_partir_de_hoje()
    {
        var barbearia = await CriarBarbearia(Plano.Basico);
        Relogio.Advance(TimeSpan.FromDays(17)); // hoje: 10/12, grátis venceu em 07/12

        var resultado = await Assinatura.Checkout(barbearia.Id, Plano.Basico, Cartao);

        Assert.Equal(new DateOnly(2027, 1, 10), resultado.Valor!.AssinaturaValidaAte);
        Assert.Equal(49.90m, Assert.Single(_gateway.Cobrancas).Valor);
    }

    [Fact]
    public async Task Pagamento_recusado_mantem_a_assinatura_como_estava_e_fica_no_historico()
    {
        var barbearia = await CriarBarbearia(Plano.Basico);
        _gateway.Aprovar = false;

        var resultado = await Assinatura.Checkout(barbearia.Id, Plano.Pro, Cartao);

        Assert.False(resultado.Sucesso);
        Assert.Equal("Pagamento recusado: Cartão sem limite.", resultado.Erro);
        var atual = (await new CadastroService(NovoContexto(), Relogio).Autenticar("dono1@teste.com", "segredo123"))!;
        Assert.Equal((Plano.Basico, true, new DateOnly(2026, 12, 7)), (atual.Plano, atual.EmPeriodoGratis, atual.AssinaturaValidaAte));
        var historico = Assert.Single(await Assinatura.Historico(barbearia.Id));
        Assert.False(historico.Aprovado);
    }

    [Fact]
    public async Task Nao_troca_para_plano_com_menos_vagas_que_os_barbeiros_ativos()
    {
        var barbearia = await CriarBarbearia(Plano.Pro);
        var catalogo = new CatalogoService(Db);
        foreach (var nome in new[] { "Zé", "Tião", "Bené" })
            await catalogo.CadastrarBarbeiro(barbearia.Id, nome);

        var resultado = await Assinatura.Checkout(barbearia.Id, Plano.Basico, Cartao);

        Assert.False(resultado.Sucesso);
        Assert.Equal("Você tem 3 barbeiros ativos e o plano Básico permite até 1. Inative barbeiros antes de trocar de plano.", resultado.Erro);
        Assert.Empty(_gateway.Cobrancas); // nada foi cobrado
    }
}
