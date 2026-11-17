using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages.Painel;

public class IndexModel(AppDbContext db, AgendamentoService agenda, TimeProvider relogio) : PageModel
{
    public Barbearia Barbearia { get; private set; } = null!;
    public DateOnly Hoje { get; private set; }
    public List<Agendamento> AgendaDeHoje { get; private set; } = [];
    public ResumoMensal Resumo { get; private set; } = new(0, 0);
    public string LinkPublico { get; private set; } = "";

    public async Task OnGetAsync()
    {
        var barbeariaId = User.BarbeariaId();
        Hoje = DateOnly.FromDateTime(relogio.AgoraLocal());
        Barbearia = (await db.Barbearias.FindAsync(barbeariaId))!;
        AgendaDeHoje = await agenda.AgendaDoDia(barbeariaId, Hoje);
        Resumo = await agenda.ResumoDoMes(barbeariaId, Hoje);
        LinkPublico = $"{Request.Scheme}://{Request.Host}/b/{Barbearia.Slug}";
    }
}
