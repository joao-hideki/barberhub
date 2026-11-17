using BarberHub.Tests.Infra;
using BarberHub.Web.Domain;
using BarberHub.Web.Services;

namespace BarberHub.Tests;

public class CadastroServiceTests(PostgresFixture banco) : TesteComBanco(banco)
{
    private CadastroService Servico => new(Db, Relogio);

    [Fact]
    public async Task Barbearia_cadastrada_comeca_com_14_dias_gratis_no_plano_escolhido()
    {
        var resultado = await Servico.CadastrarBarbearia(
            new NovaBarbearia("Barbearia do Zé", "ze@barbearia.com", "segredo123", Plano.Pro));

        Assert.True(resultado.Sucesso);
        Assert.Equal(Plano.Pro, resultado.Valor!.Plano);
        Assert.True(resultado.Valor.EmPeriodoGratis);
        Assert.Equal(new DateOnly(2026, 12, 7), resultado.Valor.AssinaturaValidaAte); // hoje (23/11) + 14 dias
    }

    [Fact]
    public async Task Endereco_publico_e_gerado_a_partir_do_nome_sem_acentos_e_simbolos()
    {
        var resultado = await Servico.CadastrarBarbearia(
            new NovaBarbearia("  Barbearia do Zé & Filhos! ", "ze@barbearia.com", "segredo123", Plano.Basico));

        Assert.Equal("barbearia-do-ze-filhos", resultado.Valor!.Slug);
    }

    [Fact]
    public async Task Endereco_publico_repetido_ganha_sufixo_numerico()
    {
        await Servico.CadastrarBarbearia(new NovaBarbearia("Corte Fino", "a@corte.com", "segredo123", Plano.Basico));
        await Servico.CadastrarBarbearia(new NovaBarbearia("Corte Fino", "b@corte.com", "segredo123", Plano.Basico));
        var terceira = await Servico.CadastrarBarbearia(new NovaBarbearia("Corte Fino", "c@corte.com", "segredo123", Plano.Basico));

        Assert.Equal("corte-fino-3", terceira.Valor!.Slug);
    }
}
