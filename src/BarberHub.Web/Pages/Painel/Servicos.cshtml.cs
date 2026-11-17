using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages.Painel;

public class ServicosModel(CatalogoService catalogo) : PageModel
{
    public List<Servico> Servicos { get; private set; } = [];

    public async Task OnGetAsync() => Servicos = await catalogo.ListarServicosAtivos(User.BarbeariaId());

    public async Task<IActionResult> OnPostCriarAsync(string nome, string preco, int duracao)
    {
        var resultado = await catalogo.CadastrarServico(User.BarbeariaId(), nome ?? "", Formulario.Decimal(preco) ?? 0, duracao);
        return Voltar(resultado.Sucesso, $"Serviço \"{nome}\" cadastrado.", resultado.Erro);
    }

    public async Task<IActionResult> OnPostSalvarAsync(int id, string nome, string preco, int duracao)
    {
        var resultado = await catalogo.AtualizarServico(User.BarbeariaId(), id, nome ?? "", Formulario.Decimal(preco) ?? 0, duracao);
        return Voltar(resultado.Sucesso, "Serviço atualizado. Agendamentos já feitos mantêm o preço antigo.", resultado.Erro);
    }

    public async Task<IActionResult> OnPostInativarAsync(int id)
    {
        var resultado = await catalogo.InativarServico(User.BarbeariaId(), id);
        return Voltar(resultado.Sucesso, "Serviço removido do catálogo.", resultado.Erro);
    }

    private IActionResult Voltar(bool sucesso, string mensagem, string? erro)
    {
        TempData[sucesso ? "Sucesso" : "Erro"] = sucesso ? mensagem : erro;
        return RedirectToPage();
    }
}
