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

    /// <summary>Alterar o preço não muda agendamentos já feitos: eles guardam o preço da época.</summary>
    public async Task<Resultado<Servico>> AtualizarServico(int barbeariaId, int servicoId, string nome, decimal preco, int duracaoMinutos)
    {
        var servico = await BuscarServico(barbeariaId, servicoId);
        if (servico is null)
            return Resultado<Servico>.Falha("Serviço não encontrado.");
        var erro = ValidarServico(nome, preco, duracaoMinutos);
        if (erro is not null)
            return Resultado<Servico>.Falha(erro);

        servico.Nome = nome.Trim();
        servico.Preco = preco;
        servico.DuracaoMinutos = duracaoMinutos;
        await db.SaveChangesAsync();
        return Resultado<Servico>.Ok(servico);
    }

    public async Task<Resultado<Servico>> InativarServico(int barbeariaId, int servicoId)
    {
        var servico = await BuscarServico(barbeariaId, servicoId);
        if (servico is null)
            return Resultado<Servico>.Falha("Serviço não encontrado.");

        servico.Ativo = false;
        await db.SaveChangesAsync();
        return Resultado<Servico>.Ok(servico);
    }

    /// <summary>Multi-tenant: só encontra o serviço se ele for da barbearia informada.</summary>
    private Task<Servico?> BuscarServico(int barbeariaId, int servicoId) =>
        db.Servicos.SingleOrDefaultAsync(s => s.Id == servicoId && s.BarbeariaId == barbeariaId);

    /// <summary>O número de barbeiros ativos é limitado pelo plano contratado.</summary>
    public async Task<Resultado<Barbeiro>> CadastrarBarbeiro(int barbeariaId, string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Resultado<Barbeiro>.Falha("Informe o nome do barbeiro.");

        var barbearia = await db.Barbearias.SingleAsync(b => b.Id == barbeariaId);
        var plano = Planos.Info(barbearia.Plano);
        var ativos = await db.Barbeiros.CountAsync(b => b.BarbeariaId == barbeariaId && b.Ativo);
        if (ativos >= plano.LimiteBarbeiros)
            return Resultado<Barbeiro>.Falha(
                $"Seu plano {plano.Nome} permite até {plano.LimiteBarbeiros} barbeiro(s). Faça upgrade de plano para cadastrar mais.");

        var barbeiro = new Barbeiro { BarbeariaId = barbeariaId, Nome = nome.Trim() };
        db.Barbeiros.Add(barbeiro);
        await db.SaveChangesAsync();
        return Resultado<Barbeiro>.Ok(barbeiro);
    }

    public Task<List<Barbeiro>> ListarBarbeirosAtivos(int barbeariaId) =>
        db.Barbeiros.Where(b => b.BarbeariaId == barbeariaId && b.Ativo).OrderBy(b => b.Nome).ToListAsync();

    public async Task<Resultado<Barbeiro>> InativarBarbeiro(int barbeariaId, int barbeiroId)
    {
        var barbeiro = await db.Barbeiros.SingleOrDefaultAsync(b => b.Id == barbeiroId && b.BarbeariaId == barbeariaId);
        if (barbeiro is null)
            return Resultado<Barbeiro>.Falha("Barbeiro não encontrado.");

        barbeiro.Ativo = false;
        await db.SaveChangesAsync();
        return Resultado<Barbeiro>.Ok(barbeiro);
    }

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
