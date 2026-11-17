namespace BarberHub.Web.Domain;

/// <summary>Histórico das cobranças de assinatura (aprovadas e recusadas).</summary>
public class Pagamento
{
    public int Id { get; set; }
    public int BarbeariaId { get; set; }
    public Plano Plano { get; set; }
    public decimal Valor { get; set; }
    public bool Aprovado { get; set; }
    public string? TransacaoId { get; set; }
    public string Mensagem { get; set; } = "";
    public DateTime Data { get; set; }
}
