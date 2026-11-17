namespace BarberHub.Web.Pagamentos;

public record DadosCartao(string Numero, string NomeTitular, string Validade, string Cvv);

public record Cobranca(decimal Valor, string Descricao, DadosCartao Cartao);

public record RespostaGateway(bool Aprovado, string? TransacaoId, string Mensagem);

/// <summary>
/// Porta para o gateway de pagamento. No MVP usamos um simulador; em produção basta
/// registrar outra implementação (ex.: Mercado Pago, Asaas, Stripe) na injeção de dependência.
/// </summary>
public interface IGatewayPagamento
{
    Task<RespostaGateway> Cobrar(Cobranca cobranca);
}
