using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages;

public class CadastroModel(CadastroService cadastro) : PageModel
{
    [BindProperty] public string Nome { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Senha { get; set; } = "";
    [BindProperty] public Plano Plano { get; set; } = Plano.Basico;

    public string? Erro { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await cadastro.CadastrarBarbearia(new NovaBarbearia(Nome, Email, Senha, Plano));
        if (!resultado.Sucesso)
        {
            Erro = resultado.Erro;
            return Page();
        }

        await Sessao.Entrar(HttpContext, resultado.Valor!);
        TempData["Sucesso"] = $"Bem-vindo! Seu período grátis vai até {resultado.Valor!.AssinaturaValidaAte:dd/MM/yyyy}. Cadastre seus barbeiros e serviços para começar.";
        return RedirectToPage("/Painel/Index");
    }
}
