using System.Globalization;
using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Pages.Publico;

/// <summary>
/// Página pública da barbearia (/b/{slug}): o cliente agenda sem login, informando nome e celular.
/// Etapas: escolher serviço/barbeiro/dia -> escolher horário e informar dados -> conferir -> confirmar.
/// </summary>
public class AgendarModel(AppDbContext db, CatalogoService catalogo, AgendamentoService agenda, TimeProvider relogio) : PageModel
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    [BindProperty(SupportsGet = true)] public int? ServicoId { get; set; }
    [BindProperty(SupportsGet = true)] public int? BarbeiroId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Data { get; set; }
    [BindProperty] public string? Hora { get; set; }
    [BindProperty] public string Nome { get; set; } = "";
    [BindProperty] public string Telefone { get; set; } = "";

    public Barbearia Barbearia { get; private set; } = null!;
    public bool AssinaturaAtiva { get; private set; }
    public List<Servico> Servicos { get; private set; } = [];
    public List<Barbeiro> Barbeiros { get; private set; } = [];
    public List<DateOnly> DiasAbertos { get; private set; } = [];
    public List<TimeOnly>? Horarios { get; private set; }
    public bool Revisando { get; private set; }
    public string? Erro { get; private set; }

    public Servico? ServicoEscolhido => Servicos.FirstOrDefault(s => s.Id == ServicoId);
    public Barbeiro? BarbeiroEscolhido => Barbeiros.FirstOrDefault(b => b.Id == BarbeiroId);
    public DateOnly? DiaEscolhido => Formulario.Data(Data);
    public string TelefoneFormatado => Domain.Telefone.Normalizar(Telefone) is { Sucesso: true } t ? Domain.Telefone.Formatar(t.Valor!) : Telefone;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (!await Carregar(slug)) return NotFound();
        await CarregarHorarios();
        return Page();
    }

    public async Task<IActionResult> OnPostRevisarAsync(string slug)
    {
        if (!await Carregar(slug)) return NotFound();
        await CarregarHorarios();

        var telefone = Domain.Telefone.Normalizar(Telefone);
        if (Formulario.Hora(Hora) is null) Erro = "Escolha um horário.";
        else if (string.IsNullOrWhiteSpace(Nome)) Erro = "Informe seu nome.";
        else if (!telefone.Sucesso) Erro = telefone.Erro;
        else Revisando = true;

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmarAsync(string slug)
    {
        if (!await Carregar(slug)) return NotFound();

        var inicio = DiaEscolhido?.ToDateTime(Formulario.Hora(Hora) ?? TimeOnly.MinValue) ?? DateTime.MinValue;
        var resultado = await agenda.Agendar(new NovoAgendamento(Barbearia.Id, BarbeiroId ?? 0, ServicoId ?? 0, inicio, Nome, Telefone));
        if (!resultado.Sucesso)
        {
            Erro = resultado.Erro;
            await CarregarHorarios();
            return Page();
        }

        TempData["Agendado"] =
            $"{ServicoEscolhido!.Nome} com {BarbeiroEscolhido!.Nome} em {inicio.ToString("dddd, dd/MM 'às' HH:mm", PtBr)}. " +
            $"A barbearia vai confirmar pelo WhatsApp {TelefoneFormatado}.";
        return RedirectToPage(new { slug });
    }

    private async Task<bool> Carregar(string slug)
    {
        var barbearia = await db.Barbearias.SingleOrDefaultAsync(b => b.Slug == slug);
        if (barbearia is null) return false;

        Barbearia = barbearia;
        var hoje = DateOnly.FromDateTime(relogio.AgoraLocal());
        AssinaturaAtiva = barbearia.AssinaturaAtiva(hoje);
        Servicos = await catalogo.ListarServicosAtivos(barbearia.Id);
        Barbeiros = await catalogo.ListarBarbeirosAtivos(barbearia.Id);

        var abertos = (await catalogo.ObterExpediente(barbearia.Id)).Select(h => h.DiaSemana).ToHashSet();
        DiasAbertos = Enumerable.Range(0, 14).Select(hoje.AddDays).Where(d => abertos.Contains(d.DayOfWeek)).ToList();
        return true;
    }

    private async Task CarregarHorarios()
    {
        if (ServicoEscolhido is not null && BarbeiroEscolhido is not null && DiaEscolhido is { } dia)
            Horarios = await agenda.HorariosDisponiveis(Barbearia.Id, BarbeiroEscolhido.Id, ServicoEscolhido.Id, dia);
    }

    public static string NomeDia(DateOnly dia) => dia.ToString("ddd, dd/MM", PtBr);
}
