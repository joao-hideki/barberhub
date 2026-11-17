namespace BarberHub.Web.Pagamentos;

public class GatewayPagamentoSimulado(TimeProvider relogio) : IGatewayPagamento
{
    public Task<RespostaGateway> Cobrar(Cobranca cobranca) => throw new NotImplementedException();
}
