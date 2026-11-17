using System.Globalization;
using BarberHub.Web.Domain;

namespace BarberHub.Web.Pagamentos;

/// <summary>
/// Simulador de gateway para a demonstração (sem conta, chave de API ou internet).
/// Cartão com número terminado em 0000 = recusado; qualquer outro cartão válido = aprovado.
/// </summary>
public class GatewayPagamentoSimulado(TimeProvider relogio) : IGatewayPagamento
{
    public Task<RespostaGateway> Cobrar(Cobranca cobranca)
    {
        var cartao = cobranca.Cartao;
        var numero = new string((cartao.Numero ?? "").Where(char.IsDigit).ToArray());

        if (numero.Length is < 13 or > 19 || !CvvValido(cartao.Cvv) || !ValidadeOk(cartao.Validade))
            return Task.FromResult(new RespostaGateway(false, null, "Dados do cartão inválidos."));

        if (numero.EndsWith("0000"))
            return Task.FromResult(new RespostaGateway(false, null, "Cartão recusado pela operadora (simulação)."));

        var transacao = "SIM-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return Task.FromResult(new RespostaGateway(true, transacao, "Pagamento aprovado."));
    }

    private static bool CvvValido(string? cvv) => cvv is { Length: 3 or 4 } && cvv.All(char.IsDigit);

    /// <summary>Validade no formato MM/AA, ainda não vencida.</summary>
    private bool ValidadeOk(string? validade)
    {
        if (!DateTime.TryParseExact(validade?.Trim(), "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mes))
            return false;
        var hoje = relogio.AgoraLocal();
        return mes.Year > hoje.Year || (mes.Year == hoje.Year && mes.Month >= hoje.Month);
    }
}
