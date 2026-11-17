using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

public record NovoAgendamento(int BarbeariaId, int BarbeiroId, int ServicoId, DateTime Inicio, string NomeCliente, string Telefone);

public class AgendamentoService(AppDbContext db, TimeProvider relogio)
{
    public async Task<Resultado<Agendamento>> Agendar(NovoAgendamento pedido)
    {
        var servico = await db.Servicos.SingleAsync(s => s.Id == pedido.ServicoId);
        var telefone = Telefone.Normalizar(pedido.Telefone).Valor!;
        var inicio = pedido.Inicio;
        var fim = inicio.AddMinutes(servico.DuracaoMinutos);

        if (await BarbeiroOcupado(pedido.BarbeiroId, inicio, fim))
            return Resultado<Agendamento>.Falha("Este horário não está mais disponível para este barbeiro.");

        var cliente = new Cliente { BarbeariaId = pedido.BarbeariaId, Nome = pedido.NomeCliente.Trim(), Telefone = telefone };
        var agendamento = new Agendamento
        {
            BarbeariaId = pedido.BarbeariaId,
            BarbeiroId = pedido.BarbeiroId,
            ServicoId = servico.Id,
            Cliente = cliente,
            Inicio = inicio,
            Fim = fim,
            Preco = servico.Preco
        };
        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();
        return Resultado<Agendamento>.Ok(agendamento);
    }

    /// <summary>Dois intervalos [a, b) e [c, d) se sobrepõem quando a &lt; d e c &lt; b. Cancelados não ocupam a agenda.</summary>
    private Task<bool> BarbeiroOcupado(int barbeiroId, DateTime inicio, DateTime fim) =>
        db.Agendamentos.AnyAsync(a => a.BarbeiroId == barbeiroId
                                      && a.Status != StatusAgendamento.Cancelado
                                      && a.Inicio < fim && inicio < a.Fim);
}
