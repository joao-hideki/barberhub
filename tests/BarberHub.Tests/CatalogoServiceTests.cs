using BarberHub.Tests.Infra;
using BarberHub.Web.Domain;
using BarberHub.Web.Services;

namespace BarberHub.Tests;

public class CatalogoServiceTests(PostgresFixture banco) : TesteComBanco(banco)
{
    private CatalogoService Catalogo => new(Db);

    [Fact]
    public async Task Servico_cadastrado_aparece_no_catalogo_com_preco_e_duracao()
    {
        var barbearia = await CriarBarbearia();

        var resultado = await Catalogo.CadastrarServico(barbearia.Id, "Corte + barba", 65.00m, 45);

        Assert.True(resultado.Sucesso);
        var servico = Assert.Single(await Catalogo.ListarServicosAtivos(barbearia.Id));
        Assert.Equal("Corte + barba", servico.Nome);
        Assert.Equal(65.00m, servico.Preco);
        Assert.Equal(45, servico.DuracaoMinutos);
    }

    [Theory]
    [InlineData("", 40, 30, "Informe o nome do serviço.")]
    [InlineData("Corte", 0, 30, "O preço deve ser maior que zero.")]
    [InlineData("Corte", 40, 0, "A duração deve ser em blocos de 15 minutos (15, 30, 45...).")]
    [InlineData("Corte", 40, 20, "A duração deve ser em blocos de 15 minutos (15, 30, 45...).")]
    public async Task Servico_com_dados_invalidos_e_recusado(string nome, decimal preco, int duracao, string erro)
    {
        var barbearia = await CriarBarbearia();

        var resultado = await Catalogo.CadastrarServico(barbearia.Id, nome, preco, duracao);

        Assert.False(resultado.Sucesso);
        Assert.Equal(erro, resultado.Erro);
        Assert.Empty(await Catalogo.ListarServicosAtivos(barbearia.Id));
    }
}
