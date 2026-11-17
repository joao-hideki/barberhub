using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

public class CatalogoService(AppDbContext db)
{
    public async Task<Resultado<Servico>> CadastrarServico(int barbeariaId, string nome, decimal preco, int duracaoMinutos)
    {
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
}
