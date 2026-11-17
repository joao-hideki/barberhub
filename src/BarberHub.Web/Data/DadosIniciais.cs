using BarberHub.Web.Domain;
using BarberHub.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Data;

/// <summary>Aplica as migrations e cria uma barbearia de demonstração na primeira execução.</summary>
public static class DadosIniciais
{
    public const string EmailDemo = "demo@barberhub.com";
    public const string SenhaDemo = "demo123";

    public static async Task PrepararBanco(IServiceProvider servicos)
    {
        using var escopo = servicos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        if (await db.Barbearias.AnyAsync())
            return;

        var cadastro = escopo.ServiceProvider.GetRequiredService<CadastroService>();
        var catalogo = escopo.ServiceProvider.GetRequiredService<CatalogoService>();
        var agenda = escopo.ServiceProvider.GetRequiredService<AgendamentoService>();
        var relogio = escopo.ServiceProvider.GetRequiredService<TimeProvider>();

        var demo = (await cadastro.CadastrarBarbearia(new NovaBarbearia("Barbearia do Zé", EmailDemo, SenhaDemo, Plano.Pro))).Valor!;
        var barbeiros = new[]
        {
            (await catalogo.CadastrarBarbeiro(demo.Id, "Zé")).Valor!,
            (await catalogo.CadastrarBarbeiro(demo.Id, "Tião")).Valor!
        };
        var servicos_ = new[]
        {
            (await catalogo.CadastrarServico(demo.Id, "Corte", 40m, 30)).Valor!,
            (await catalogo.CadastrarServico(demo.Id, "Barba", 30m, 30)).Valor!,
            (await catalogo.CadastrarServico(demo.Id, "Corte + barba", 65m, 60)).Valor!
        };

        var clientes = new (string Nome, string Telefone)[]
        {
            ("Carlos Souza", "11987654321"), ("Pedro Lima", "11976543210"),
            ("Rafael Costa", "21998765432"), ("Lucas Alves", "31991234567")
        };

        // Alguns horários nos próximos dias para a agenda não começar vazia.
        var hoje = DateOnly.FromDateTime(relogio.AgoraLocal());
        var i = 0;
        for (var d = 0; d < 3; d++)
        {
            foreach (var barbeiro in barbeiros)
            {
                var cliente = clientes[i % clientes.Length];
                var servico = servicos_[i % servicos_.Length];
                var livres = await agenda.HorariosDisponiveis(demo.Id, barbeiro.Id, servico.Id, hoje.AddDays(d));
                if (livres.Count > 4)
                {
                    var inicio = hoje.AddDays(d).ToDateTime(livres[(i * 3 + 2) % livres.Count]);
                    await agenda.Agendar(new NovoAgendamento(demo.Id, barbeiro.Id, servico.Id, inicio, cliente.Nome, cliente.Telefone));
                }
                i++;
            }
        }
    }
}
