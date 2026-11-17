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
}
