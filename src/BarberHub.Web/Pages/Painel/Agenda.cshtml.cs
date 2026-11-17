using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages.Painel;

public class AgendaModel(AppDbContext db, AgendamentoService agenda, TimeProvider relogio) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Data { get; set; }

    public DateOnly Dia { get; private set; }
    public string NomeBarbearia { get; private set; } = "";
    public List<Agendamento> Atendimentos { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Dia = Formulario.Data(Data) ?? DateOnly.FromDateTime(relogio.AgoraLocal());
        NomeBarbearia = (await db.Barbearias.FindAsync(User.BarbeariaId()))!.Nome;
        Atendimentos = await agenda.AgendaDoDia(User.BarbeariaId(), Dia);
    }

    public Task<IActionResult> OnPostConfirmarAsync(int id) => Executar(agenda.Confirmar(User.BarbeariaId(), id), "Atendimento confirmado.");
    public Task<IActionResult> OnPostConcluirAsync(int id) => Executar(agenda.Concluir(User.BarbeariaId(), id), "Atendimento concluído.");
    public Task<IActionResult> OnPostCancelarAsync(int id) => Executar(agenda.Cancelar(User.BarbeariaId(), id), "Atendimento cancelado. O horário foi liberado.");

    private async Task<IActionResult> Executar(Task<Resultado<Agendamento>> acao, string mensagem)
    {
        var resultado = await acao;
        TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? mensagem : resultado.Erro;
        return RedirectToPage(new { Data });
    }

    /// <summary>Link do WhatsApp com a mensagem de confirmação pronta (sem API, sem custo).</summary>
    public string LinkWhatsApp(Agendamento a) =>
        $"https://wa.me/55{a.Cliente.Telefone}?text=" + Uri.EscapeDataString(
            $"Olá, {a.Cliente.Nome}! Aqui é da {NomeBarbearia}. Confirmando seu horário: {a.Servico.Nome} com {a.Barbeiro.Nome} " +
            $"em {a.Inicio:dd/MM} às {a.Inicio:HH:mm}. Podemos confirmar?");
}
