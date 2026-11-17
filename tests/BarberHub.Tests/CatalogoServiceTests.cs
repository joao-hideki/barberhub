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

    [Fact]
    public async Task Servico_inativado_sai_do_catalogo()
    {
        var barbearia = await CriarBarbearia();
        var corte = (await Catalogo.CadastrarServico(barbearia.Id, "Corte", 40m, 30)).Valor!;
        await Catalogo.CadastrarServico(barbearia.Id, "Barba", 30m, 30);

        var resultado = await Catalogo.InativarServico(barbearia.Id, corte.Id);

        Assert.True(resultado.Sucesso);
        var restante = Assert.Single(await Catalogo.ListarServicosAtivos(barbearia.Id));
        Assert.Equal("Barba", restante.Nome);
    }

    [Fact]
    public async Task Barbearia_nao_altera_servico_de_outra_barbearia()
    {
        var minha = await CriarBarbearia();
        var outra = await CriarBarbearia();
        var servicoDaOutra = (await Catalogo.CadastrarServico(outra.Id, "Corte", 40m, 30)).Valor!;

        var inativar = await Catalogo.InativarServico(minha.Id, servicoDaOutra.Id);
        var atualizar = await Catalogo.AtualizarServico(minha.Id, servicoDaOutra.Id, "Corte", 1m, 15);

        Assert.False(inativar.Sucesso);
        Assert.False(atualizar.Sucesso);
        Assert.Equal("Serviço não encontrado.", inativar.Erro);
        var intacto = Assert.Single(await Catalogo.ListarServicosAtivos(outra.Id));
        Assert.Equal(40m, intacto.Preco);
    }

    [Fact]
    public async Task Preco_e_duracao_do_servico_podem_ser_alterados()
    {
        var barbearia = await CriarBarbearia();
        var corte = (await Catalogo.CadastrarServico(barbearia.Id, "Corte", 40m, 30)).Valor!;

        var resultado = await Catalogo.AtualizarServico(barbearia.Id, corte.Id, "Corte degradê", 50m, 45);

        Assert.True(resultado.Sucesso);
        var atualizado = Assert.Single(await Catalogo.ListarServicosAtivos(barbearia.Id));
        Assert.Equal(("Corte degradê", 50m, 45), (atualizado.Nome, atualizado.Preco, atualizado.DuracaoMinutos));
    }

    [Fact]
    public async Task Plano_basico_permite_apenas_um_barbeiro()
    {
        var barbearia = await CriarBarbearia(Plano.Basico);

        var primeiro = await Catalogo.CadastrarBarbeiro(barbearia.Id, "Zé");
        var segundo = await Catalogo.CadastrarBarbeiro(barbearia.Id, "Tião");

        Assert.True(primeiro.Sucesso);
        Assert.False(segundo.Sucesso);
        Assert.Equal("Seu plano Básico permite até 1 barbeiro(s). Faça upgrade de plano para cadastrar mais.", segundo.Erro);
        Assert.Single(await Catalogo.ListarBarbeirosAtivos(barbearia.Id));
    }

    [Fact]
    public async Task Plano_pro_permite_ate_cinco_barbeiros()
    {
        var barbearia = await CriarBarbearia(Plano.Pro);
        for (var i = 1; i <= 5; i++)
            Assert.True((await Catalogo.CadastrarBarbeiro(barbearia.Id, $"Barbeiro {i}")).Sucesso);

        var sexto = await Catalogo.CadastrarBarbeiro(barbearia.Id, "Barbeiro 6");

        Assert.False(sexto.Sucesso);
        Assert.Equal(5, (await Catalogo.ListarBarbeirosAtivos(barbearia.Id)).Count);
    }

    [Fact]
    public async Task Barbeiro_inativado_libera_vaga_do_plano()
    {
        var barbearia = await CriarBarbearia(Plano.Basico);
        var ze = (await Catalogo.CadastrarBarbeiro(barbearia.Id, "Zé")).Valor!;

        await Catalogo.InativarBarbeiro(barbearia.Id, ze.Id);
        var tiao = await Catalogo.CadastrarBarbeiro(barbearia.Id, "Tião");

        Assert.True(tiao.Sucesso);
        Assert.Equal("Tião", Assert.Single(await Catalogo.ListarBarbeirosAtivos(barbearia.Id)).Nome);
    }
}
