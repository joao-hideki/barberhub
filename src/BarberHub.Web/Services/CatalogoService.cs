using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

public class CatalogoService(AppDbContext db)
{
    /// <summary>A agenda é dividida em blocos de 15 minutos.</summary>
    public const int BlocoMinutos = 15;

    public async Task<Resultado<Servico>> CadastrarServico(int barbeariaId, string nome, decimal preco, int duracaoMinutos)
    {
        var erro = ValidarServico(nome, preco, duracaoMinutos);
        if (erro is not null)
            return Resultado<Servico>.Falha(erro);

        var servico = new Servico
        {
            BarbeariaId = barbeariaId,
            Nome = nome.Trim(),
            Preco = preco,
            DuracaoMinutos = duracaoMinutos
        };
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();
        return Resultado<Servico>.Ok(servico);
    }

    public Task<List<Servico>> ListarServicosAtivos(int barbeariaId) =>
        db.Servicos.Where(s => s.BarbeariaId == barbeariaId && s.Ativo).OrderBy(s => s.Nome).ToListAsync();

    private static string? ValidarServico(string nome, decimal preco, int duracaoMinutos)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return "Informe o nome do serviço.";
        if (preco <= 0)
            return "O preço deve ser maior que zero.";
        if (duracaoMinutos <= 0 || duracaoMinutos % BlocoMinutos != 0)
            return "A duração deve ser em blocos de 15 minutos (15, 30, 45...).";
        return null;
    }
}
