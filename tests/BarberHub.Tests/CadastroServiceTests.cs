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
}
