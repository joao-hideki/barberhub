using BarberHub.Tests.Infra;
using BarberHub.Web.Domain;
using BarberHub.Web.Services;

namespace BarberHub.Tests;

/// <summary>
/// Cenário: hoje é segunda 23/11/2026, 09:00. Barbearia Pro aberta seg–sáb 9h–19h,
/// barbeiros Zé e Tião, serviços Corte (30 min, R$ 40) e Corte + barba (60 min, R$ 65).
/// </summary>
public class AgendamentoServiceTests(PostgresFixture banco) : TesteComBanco(banco)
{
    private static readonly DateTime Terca = new(2026, 11, 24);

    private Barbearia _barbearia = null!;
    private Barbeiro _ze = null!, _tiao = null!;
    private Servico _corte = null!, _corteEBarba = null!;

    private AgendamentoService Agenda => new(Db, Relogio);
    private CatalogoService Catalogo => new(Db);

    private async Task PrepararBarbearia()
    {
        _barbearia = await CriarBarbearia(Plano.Pro);
        _ze = (await Catalogo.CadastrarBarbeiro(_barbearia.Id, "Zé")).Valor!;
        _tiao = (await Catalogo.CadastrarBarbeiro(_barbearia.Id, "Tião")).Valor!;
        _corte = (await Catalogo.CadastrarServico(_barbearia.Id, "Corte", 40m, 30)).Valor!;
        _corteEBarba = (await Catalogo.CadastrarServico(_barbearia.Id, "Corte + barba", 65m, 60)).Valor!;
    }

    private Task<Resultado<Agendamento>> Agendar(Barbeiro barbeiro, Servico servico, DateTime inicio,
        string telefone = "(11) 98765-4321", string nome = "Carlos") =>
        Agenda.Agendar(new NovoAgendamento(_barbearia.Id, barbeiro.Id, servico.Id, inicio, nome, telefone));

    [Fact]
    public async Task Horario_livre_e_agendado_bloqueando_a_duracao_do_servico()
    {
        await PrepararBarbearia();

        var resultado = await Agendar(_ze, _corteEBarba, Terca.AddHours(10));

        Assert.True(resultado.Sucesso, resultado.Erro);
        var agendamento = resultado.Valor!;
        Assert.Equal(StatusAgendamento.Agendado, agendamento.Status);
        Assert.Equal(Terca.AddHours(10), agendamento.Inicio);
        Assert.Equal(Terca.AddHours(11), agendamento.Fim);
        Assert.Equal(65m, agendamento.Preco);
    }
}
