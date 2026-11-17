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

    [Theory]
    [InlineData(10, 0)]   // mesmo início
    [InlineData(10, 30)]  // começa no meio do atendimento das 10h às 11h
    [InlineData(9, 30)]   // corte+barba das 9h30 invadiria as 10h
    public async Task Horario_que_sobrepoe_atendimento_do_mesmo_barbeiro_e_recusado(int hora, int minuto)
    {
        await PrepararBarbearia();
        await Agendar(_ze, _corteEBarba, Terca.AddHours(10), telefone: "11911111111");

        var resultado = await Agendar(_ze, _corteEBarba, Terca.AddHours(hora).AddMinutes(minuto), telefone: "11922222222");

        Assert.False(resultado.Sucesso);
        Assert.Equal("Este horário não está mais disponível para este barbeiro.", resultado.Erro);
    }

    [Fact]
    public async Task Horario_encostado_no_fim_do_anterior_ou_com_outro_barbeiro_e_aceito()
    {
        await PrepararBarbearia();
        await Agendar(_ze, _corteEBarba, Terca.AddHours(10), telefone: "11911111111");

        var logoDepois = await Agendar(_ze, _corte, Terca.AddHours(11), telefone: "11922222222");
        var outroBarbeiro = await Agendar(_tiao, _corte, Terca.AddHours(10), telefone: "11933333333");

        Assert.True(logoDepois.Sucesso, logoDepois.Erro);
        Assert.True(outroBarbeiro.Sucesso, outroBarbeiro.Erro);
    }

    [Theory]
    [InlineData("2026-11-24 18:30")] // corte+barba terminaria 19h30, depois do fechamento (19h)
    [InlineData("2026-11-24 08:30")] // antes de abrir
    [InlineData("2026-11-29 10:00")] // domingo: fechado
    public async Task Atendimento_precisa_caber_inteiro_no_expediente(string inicio)
    {
        await PrepararBarbearia();

        var resultado = await Agendar(_ze, _corteEBarba, DateTime.Parse(inicio));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Horário fora do expediente da barbearia.", resultado.Erro);
    }

    [Fact]
    public async Task Atendimento_que_termina_exatamente_no_fechamento_e_aceito()
    {
        await PrepararBarbearia();

        var resultado = await Agendar(_ze, _corteEBarba, Terca.AddHours(18));

        Assert.True(resultado.Sucesso, resultado.Erro);
    }

    [Fact]
    public async Task Nao_agenda_no_passado()
    {
        await PrepararBarbearia(); // agora: segunda 23/11, 09:00

        var resultado = await Agendar(_ze, _corte, new DateTime(2026, 11, 23, 8, 45, 0));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Não é possível agendar em um horário que já passou.", resultado.Erro);
    }

    [Fact]
    public async Task Horario_precisa_estar_em_blocos_de_15_minutos()
    {
        await PrepararBarbearia();

        var resultado = await Agendar(_ze, _corte, Terca.AddHours(10).AddMinutes(10));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Escolha um horário em blocos de 15 minutos (ex.: 10:00, 10:15).", resultado.Erro);
    }

    [Fact]
    public async Task Telefone_invalido_impede_o_agendamento()
    {
        await PrepararBarbearia();

        var resultado = await Agendar(_ze, _corte, Terca.AddHours(10), telefone: "3265-4321");

        Assert.False(resultado.Sucesso);
        Assert.Equal(Telefone.MensagemInvalido, resultado.Erro);
    }

    [Fact]
    public async Task Mesmo_celular_em_formatos_diferentes_e_o_mesmo_cliente()
    {
        await PrepararBarbearia();

        var primeiro = await Agendar(_ze, _corte, Terca.AddHours(10), telefone: "(11) 98765-4321", nome: "Carlos");
        var segundo = await Agendar(_ze, _corte, Terca.AddHours(14), telefone: "+55 11 98765 4321", nome: "Carlos Silva");

        Assert.Equal(primeiro.Valor!.ClienteId, segundo.Valor!.ClienteId);
        Assert.Equal("11987654321", segundo.Valor.Cliente.Telefone);
    }

    [Fact]
    public async Task Cliente_nao_marca_dois_horarios_ao_mesmo_tempo_nem_com_barbeiros_diferentes()
    {
        await PrepararBarbearia();
        await Agendar(_ze, _corteEBarba, Terca.AddHours(10));

        var resultado = await Agendar(_tiao, _corte, Terca.AddHours(10).AddMinutes(30));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Você já tem um horário marcado neste período.", resultado.Erro);
    }

    [Fact]
    public async Task Celular_pode_ter_no_maximo_dois_horarios_em_aberto_na_barbearia()
    {
        await PrepararBarbearia();
        await Agendar(_ze, _corte, Terca.AddHours(10));
        await Agendar(_ze, _corte, Terca.AddHours(11));

        var terceiro = await Agendar(_ze, _corte, Terca.AddHours(12));

        Assert.False(terceiro.Sucesso);
        Assert.Equal("Você já tem 2 horários em aberto nesta barbearia. Para marcar mais, fale com a barbearia.", terceiro.Erro);
    }

    [Fact]
    public async Task Servico_ou_barbeiro_inativo_ou_de_outra_barbearia_nao_pode_ser_agendado()
    {
        await PrepararBarbearia();
        var outra = await CriarBarbearia();
        var barbeiroDeOutra = (await Catalogo.CadastrarBarbeiro(outra.Id, "Intruso")).Valor!;
        await Catalogo.InativarServico(_barbearia.Id, _corte.Id);

        var servicoInativo = await Agendar(_ze, _corte, Terca.AddHours(10));
        var barbeiroAlheio = await Agendar(barbeiroDeOutra, _corteEBarba, Terca.AddHours(10));

        Assert.Equal("Serviço ou barbeiro indisponível.", servicoInativo.Erro);
        Assert.Equal("Serviço ou barbeiro indisponível.", barbeiroAlheio.Erro);
    }

    [Fact]
    public async Task Barbearia_com_assinatura_vencida_nao_recebe_agendamentos()
    {
        await PrepararBarbearia();            // período grátis válido até 07/12
        Relogio.Advance(TimeSpan.FromDays(15)); // agora: 08/12

        var resultado = await Agendar(_ze, _corte, new DateTime(2026, 12, 9, 10, 0, 0));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Agenda indisponível: a assinatura desta barbearia está vencida.", resultado.Erro);
    }

    private static TimeOnly H(int hora, int minuto = 0) => new(hora, minuto);

    [Fact]
    public async Task Horarios_disponiveis_pulam_o_periodo_ocupado_e_respeitam_a_duracao()
    {
        await PrepararBarbearia();
        await Agendar(_ze, _corteEBarba, Terca.AddHours(10)); // Zé ocupado 10h–11h

        var horarios = await Agenda.HorariosDisponiveis(_barbearia.Id, _ze.Id, _corteEBarba.Id, DateOnly.FromDateTime(Terca));

        Assert.Equal(H(9), horarios.First());
        Assert.DoesNotContain(H(9, 15), horarios);  // 9h15–10h15 invadiria as 10h
        Assert.DoesNotContain(H(10, 30), horarios);
        Assert.Contains(H(11), horarios);
        Assert.Equal(H(18), horarios.Last());       // 18h–19h é o último que cabe
    }

    [Fact]
    public async Task Horarios_de_hoje_comecam_no_proximo_bloco_livre()
    {
        await PrepararBarbearia();
        Relogio.Advance(TimeSpan.FromMinutes(4 * 60 + 10)); // segunda, 13:10

        var horarios = await Agenda.HorariosDisponiveis(_barbearia.Id, _ze.Id, _corte.Id, new DateOnly(2026, 11, 23));

        Assert.Equal(H(13, 15), horarios.First());
    }

    [Fact]
    public async Task Dia_fechado_nao_tem_horarios()
    {
        await PrepararBarbearia();

        var domingo = await Agenda.HorariosDisponiveis(_barbearia.Id, _ze.Id, _corte.Id, new DateOnly(2026, 11, 29));

        Assert.Empty(domingo);
    }

    [Fact]
    public async Task Atendimento_segue_o_fluxo_agendado_confirmado_concluido()
    {
        await PrepararBarbearia();
        var agendamento = (await Agendar(_ze, _corte, Terca.AddHours(10))).Valor!;

        var aposConfirmar = (await Agenda.Confirmar(_barbearia.Id, agendamento.Id)).Valor!.Status;
        var aposConcluir = (await Agenda.Concluir(_barbearia.Id, agendamento.Id)).Valor!.Status;

        Assert.Equal(StatusAgendamento.Confirmado, aposConfirmar);
        Assert.Equal(StatusAgendamento.Concluido, aposConcluir);
    }

    [Fact]
    public async Task Atendimento_concluido_nao_pode_ser_cancelado_e_cancelado_nao_pode_ser_confirmado()
    {
        await PrepararBarbearia();
        var concluido = (await Agendar(_ze, _corte, Terca.AddHours(10))).Valor!;
        await Agenda.Concluir(_barbearia.Id, concluido.Id);
        var cancelado = (await Agendar(_ze, _corte, Terca.AddHours(11))).Valor!;
        await Agenda.Cancelar(_barbearia.Id, cancelado.Id);

        var cancelarConcluido = await Agenda.Cancelar(_barbearia.Id, concluido.Id);
        var confirmarCancelado = await Agenda.Confirmar(_barbearia.Id, cancelado.Id);

        Assert.Equal("Este atendimento já foi concluído.", cancelarConcluido.Erro);
        Assert.Equal("Este atendimento foi cancelado.", confirmarCancelado.Erro);
    }

    [Fact]
    public async Task Cancelar_libera_o_horario_para_outro_cliente()
    {
        await PrepararBarbearia();
        var agendamento = (await Agendar(_ze, _corte, Terca.AddHours(10), telefone: "11911111111")).Valor!;

        await Agenda.Cancelar(_barbearia.Id, agendamento.Id);
        var outroCliente = await Agendar(_ze, _corte, Terca.AddHours(10), telefone: "11922222222");

        Assert.True(outroCliente.Sucesso, outroCliente.Erro);
    }

    [Fact]
    public async Task Barbearia_nao_altera_atendimento_de_outra_barbearia()
    {
        await PrepararBarbearia();
        var agendamento = (await Agendar(_ze, _corte, Terca.AddHours(10))).Valor!;
        var outra = await CriarBarbearia();

        var resultado = await Agenda.Cancelar(outra.Id, agendamento.Id);

        Assert.Equal("Atendimento não encontrado.", resultado.Erro);
        Assert.Equal(StatusAgendamento.Agendado, Assert.Single(await Agenda.AgendaDoDia(_barbearia.Id, DateOnly.FromDateTime(Terca))).Status);
    }
}
