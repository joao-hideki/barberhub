using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Pagamentos;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

/// <summary>Cobrança da mensalidade do SaaS (o que a barbearia paga para usar o BarberHub).</summary>
public class AssinaturaService(AppDbContext db, IGatewayPagamento gateway, TimeProvider relogio)
{
    public async Task<Resultado<Barbearia>> Checkout(int barbeariaId, Plano plano, DadosCartao cartao)
    {
        var barbearia = await db.Barbearias.SingleAsync(b => b.Id == barbeariaId);
        var info = Planos.Info(plano);

        var barbeirosAtivos = await db.Barbeiros.CountAsync(b => b.BarbeariaId == barbeariaId && b.Ativo);
        if (barbeirosAtivos > info.LimiteBarbeiros)
            return Resultado<Barbearia>.Falha(
                $"Você tem {barbeirosAtivos} barbeiros ativos e o plano {info.Nome} permite até {info.LimiteBarbeiros}. " +
                "Inative barbeiros antes de trocar de plano.");

        var agora = relogio.AgoraLocal();
        var resposta = await gateway.Cobrar(new Cobranca(info.PrecoMensal, $"BarberHub - plano {info.Nome} (mensal)", cartao));

        db.Pagamentos.Add(new Pagamento
        {
            BarbeariaId = barbeariaId,
            Plano = plano,
            Valor = info.PrecoMensal,
            Aprovado = resposta.Aprovado,
            TransacaoId = resposta.TransacaoId,
            Mensagem = resposta.Mensagem,
            Data = agora
        });

        if (!resposta.Aprovado)
        {
            await db.SaveChangesAsync();
            return Resultado<Barbearia>.Falha($"Pagamento recusado: {resposta.Mensagem}");
        }

        // Renovação conta a partir do fim da validade atual (não perde dias já pagos/grátis),
        // ou a partir de hoje se a assinatura já venceu.
        var hoje = DateOnly.FromDateTime(agora);
        var inicio = barbearia.AssinaturaValidaAte > hoje ? barbearia.AssinaturaValidaAte : hoje;
        barbearia.Plano = plano;
        barbearia.EmPeriodoGratis = false;
        barbearia.AssinaturaValidaAte = inicio.AddMonths(1);
        await db.SaveChangesAsync();
        return Resultado<Barbearia>.Ok(barbearia);
    }

    public Task<List<Pagamento>> Historico(int barbeariaId) =>
        db.Pagamentos.Where(p => p.BarbeariaId == barbeariaId).OrderByDescending(p => p.Data).ToListAsync();
}
