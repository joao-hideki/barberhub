using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using BarberHub.Web.Pagamentos;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

public class AssinaturaService(AppDbContext db, IGatewayPagamento gateway, TimeProvider relogio)
{
    public async Task<Resultado<Barbearia>> Checkout(int barbeariaId, Plano plano, DadosCartao cartao)
    {
        var barbearia = await db.Barbearias.SingleAsync(b => b.Id == barbeariaId);
        var info = Planos.Info(plano);
        var agora = relogio.AgoraLocal();
        var hoje = DateOnly.FromDateTime(agora);

        var resposta = await gateway.Cobrar(new Cobranca(info.PrecoMensal, $"BarberHub - plano {info.Nome} (mensal)", cartao));

        // Renovação conta a partir do fim da validade atual (ou de hoje, se já venceu).
        var base_ = barbearia.AssinaturaValidaAte > hoje ? barbearia.AssinaturaValidaAte : hoje;
        barbearia.Plano = plano;
        barbearia.EmPeriodoGratis = false;
        barbearia.AssinaturaValidaAte = base_.AddMonths(1);
        await db.SaveChangesAsync();
        return Resultado<Barbearia>.Ok(barbearia);
    }
}
