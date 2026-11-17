using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Pagamentos;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages.Painel;

public class AssinaturaModel(AppDbContext db, AssinaturaService assinatura, TimeProvider relogio) : PageModel
{
    public Barbearia Barbearia { get; private set; } = null!;
    public DateOnly Hoje { get; private set; }
    public List<Pagamento> Historico { get; private set; } = [];

    [BindProperty] public Plano Plano { get; set; }
    [BindProperty] public string NumeroCartao { get; set; } = "";
    [BindProperty] public string NomeTitular { get; set; } = "";
    [BindProperty] public string Validade { get; set; } = "";
    [BindProperty] public string Cvv { get; set; } = "";

    public async Task OnGetAsync()
    {
        await Carregar();
        Plano = Barbearia.Plano;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await assinatura.Checkout(User.BarbeariaId(), Plano,
            new DadosCartao(NumeroCartao, NomeTitular, Validade, Cvv));

        if (resultado.Sucesso)
        {
            TempData["Sucesso"] = $"Pagamento aprovado! Plano {Planos.Info(Plano).Nome} ativo até {resultado.Valor!.AssinaturaValidaAte:dd/MM/yyyy}.";
            return RedirectToPage();
        }

        TempData["Erro"] = resultado.Erro;
        return RedirectToPage();
    }

    private async Task Carregar()
    {
        Hoje = DateOnly.FromDateTime(relogio.AgoraLocal());
        Barbearia = (await db.Barbearias.FindAsync(User.BarbeariaId()))!;
        Historico = await assinatura.Historico(User.BarbeariaId());
    }
}
