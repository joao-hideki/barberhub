using BarberHub.Web.Domain;
using BarberHub.Web.Infra;
using BarberHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BarberHub.Web.Pages.Painel;

public class ExpedienteModel(CatalogoService catalogo) : PageModel
{
    public static readonly DayOfWeek[] Semana =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    public Dictionary<DayOfWeek, HorarioFuncionamento> Dias { get; private set; } = [];

    public async Task OnGetAsync() =>
        Dias = (await catalogo.ObterExpediente(User.BarbeariaId())).ToDictionary(h => h.DiaSemana);

    public async Task<IActionResult> OnPostAsync()
    {
        var erros = new List<string>();
        foreach (var dia in Semana)
        {
            var aberto = Request.Form[$"aberto-{dia}"] == "on";
            var abre = aberto ? Formulario.Hora(Request.Form[$"abre-{dia}"]) : null;
            var fecha = aberto ? Formulario.Hora(Request.Form[$"fecha-{dia}"]) : null;
            var resultado = await catalogo.DefinirExpediente(User.BarbeariaId(), dia, abre, fecha);
            if (!resultado.Sucesso)
                erros.Add($"{Nome(dia)}: {resultado.Erro}");
        }

        TempData[erros.Count == 0 ? "Sucesso" : "Erro"] = erros.Count == 0 ? "Expediente salvo." : string.Join(" ", erros);
        return RedirectToPage();
    }

    public static string Nome(DayOfWeek dia) =>
        new System.Globalization.CultureInfo("pt-BR").DateTimeFormat.GetDayName(dia);
}
