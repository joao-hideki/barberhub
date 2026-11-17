using BarberHub.Web.Data;
using BarberHub.Web.Domain;

namespace BarberHub.Web.Services;

public record NovoAgendamento(int BarbeariaId, int BarbeiroId, int ServicoId, DateTime Inicio, string NomeCliente, string Telefone);

public class AgendamentoService(AppDbContext db, TimeProvider relogio)
{
    public Task<Resultado<Agendamento>> Agendar(NovoAgendamento pedido) => throw new NotImplementedException();
}
