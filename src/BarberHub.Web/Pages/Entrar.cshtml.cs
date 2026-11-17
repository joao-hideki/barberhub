using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages;

public class EntrarModel(CadastroService cadastro) : PageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Senha { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public string? Erro { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var barbearia = await cadastro.Autenticar(Email, Senha);
        if (barbearia is null)
        {
            Erro = "E-mail ou senha incorretos.";
            return Page();
        }

        await Sessao.Entrar(HttpContext, barbearia);
        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Painel/Index");
    }
}
