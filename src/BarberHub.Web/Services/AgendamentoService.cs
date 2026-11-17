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

        var cliente = new Cliente { BarbeariaId = pedido.BarbeariaId, Nome = pedido.NomeCliente.Trim(), Telefone = telefone };
        var agendamento = new Agendamento
        {
            BarbeariaId = pedido.BarbeariaId,
            BarbeiroId = pedido.BarbeiroId,
            ServicoId = servico.Id,
            Cliente = cliente,
            Inicio = pedido.Inicio,
            Fim = pedido.Inicio.AddMinutes(servico.DuracaoMinutos),
            Preco = servico.Preco
        };
        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();
        return Resultado<Agendamento>.Ok(agendamento);
    }
}
