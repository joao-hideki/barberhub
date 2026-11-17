using BarberHub.Web.Data;
using BarberHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberHub.Web.Services;

public record NovoAgendamento(int BarbeariaId, int BarbeiroId, int ServicoId, DateTime Inicio, string NomeCliente, string Telefone);

public class AgendamentoService(AppDbContext db, TimeProvider relogio)
{
    /// <summary>Antiabuso: evita que um número inventado ou trote trave a agenda.</summary>
    public const int MaximoEmAbertoPorCliente = 2;

    public async Task<Resultado<Agendamento>> Agendar(NovoAgendamento pedido)
    {
        var barbearia = await db.Barbearias.SingleAsync(b => b.Id == pedido.BarbeariaId);
        if (!barbearia.AssinaturaAtiva(DateOnly.FromDateTime(relogio.AgoraLocal())))
            return Resultado<Agendamento>.Falha("Agenda indisponível: a assinatura desta barbearia está vencida.");

        var servico = await db.Servicos.SingleOrDefaultAsync(s => s.Id == pedido.ServicoId && s.BarbeariaId == pedido.BarbeariaId && s.Ativo);
        var barbeiroValido = await db.Barbeiros.AnyAsync(b => b.Id == pedido.BarbeiroId && b.BarbeariaId == pedido.BarbeariaId && b.Ativo);
        if (servico is null || !barbeiroValido)
            return Resultado<Agendamento>.Falha("Serviço ou barbeiro indisponível.");
        var telefone = Telefone.Normalizar(pedido.Telefone);
        if (!telefone.Sucesso)
            return Resultado<Agendamento>.Falha(telefone.Erro!);
        var inicio = pedido.Inicio;
        var fim = inicio.AddMinutes(servico.DuracaoMinutos);

        if (inicio.Minute % CatalogoService.BlocoMinutos != 0 || inicio.Second != 0)
            return Resultado<Agendamento>.Falha("Escolha um horário em blocos de 15 minutos (ex.: 10:00, 10:15).");
        if (inicio < relogio.AgoraLocal())
            return Resultado<Agendamento>.Falha("Não é possível agendar em um horário que já passou.");
        if (!await CabeNoExpediente(pedido.BarbeariaId, inicio, fim))
            return Resultado<Agendamento>.Falha("Horário fora do expediente da barbearia.");
        if (await BarbeiroOcupado(pedido.BarbeiroId, inicio, fim))
            return Resultado<Agendamento>.Falha("Este horário não está mais disponível para este barbeiro.");

        var cliente = await db.Clientes.SingleOrDefaultAsync(c => c.BarbeariaId == pedido.BarbeariaId && c.Telefone == telefone.Valor);
        if (cliente is null)
        {
            cliente = new Cliente { BarbeariaId = pedido.BarbeariaId, Telefone = telefone.Valor! };
            db.Clientes.Add(cliente);
        }
        else
        {
            var emAberto = db.Agendamentos.Where(a => a.ClienteId == cliente.Id
                                                      && (a.Status == StatusAgendamento.Agendado || a.Status == StatusAgendamento.Confirmado));
            if (await emAberto.AnyAsync(a => a.Inicio < fim && inicio < a.Fim))
                return Resultado<Agendamento>.Falha("Você já tem um horário marcado neste período.");
            if (await emAberto.CountAsync(a => a.Inicio >= relogio.AgoraLocal()) >= MaximoEmAbertoPorCliente)
                return Resultado<Agendamento>.Falha(
                    $"Você já tem {MaximoEmAbertoPorCliente} horários em aberto nesta barbearia. Para marcar mais, fale com a barbearia.");
        }
        cliente.Nome = pedido.NomeCliente.Trim();
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

    public Task<List<TimeOnly>> HorariosDisponiveis(int barbeariaId, int barbeiroId, int servicoId, DateOnly data) =>
        throw new NotImplementedException();

    private async Task<bool> CabeNoExpediente(int barbeariaId, DateTime inicio, DateTime fim)
    {
        var dia = await db.Expedientes.SingleOrDefaultAsync(h => h.BarbeariaId == barbeariaId && h.DiaSemana == inicio.DayOfWeek);
        return dia is not null
               && fim.Date == inicio.Date
               && TimeOnly.FromDateTime(inicio) >= dia.Abre
               && TimeOnly.FromDateTime(fim) <= dia.Fecha;
    }

    /// <summary>Dois intervalos [a, b) e [c, d) se sobrepõem quando a &lt; d e c &lt; b. Cancelados não ocupam a agenda.</summary>
    private Task<bool> BarbeiroOcupado(int barbeiroId, DateTime inicio, DateTime fim) =>
        db.Agendamentos.AnyAsync(a => a.BarbeiroId == barbeiroId
                                      && a.Status != StatusAgendamento.Cancelado
                                      && a.Inicio < fim && inicio < a.Fim);
}
