using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages.Painel;

public class BarbeirosModel(AppDbContext db, CatalogoService catalogo) : PageModel
{
    public List<Barbeiro> Barbeiros { get; private set; } = [];
    public InfoPlano Plano { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Plano = Planos.Info((await db.Barbearias.FindAsync(User.BarbeariaId()))!.Plano);
        Barbeiros = await catalogo.ListarBarbeirosAtivos(User.BarbeariaId());
    }

    public async Task<IActionResult> OnPostCriarAsync(string nome)
    {
        var resultado = await catalogo.CadastrarBarbeiro(User.BarbeariaId(), nome ?? "");
        TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? $"{nome} cadastrado." : resultado.Erro;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostInativarAsync(int id)
    {
        var resultado = await catalogo.InativarBarbeiro(User.BarbeariaId(), id);
        TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Sucesso ? "Barbeiro removido." : resultado.Erro;
        return RedirectToPage();
    }
}
