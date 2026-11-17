namespace BarberHub.Web.Domain;

/// <summary>Serviço oferecido (corte, barba...). A duração define quanto tempo o horário fica bloqueado na agenda.</summary>
public class Servico
{
    public int Id { get; set; }
    public int BarbeariaId { get; set; }
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }
    public int DuracaoMinutos { get; set; }
    public bool Ativo { get; set; } = true;
}

public class Barbeiro
{
    public int Id { get; set; }
    public int BarbeariaId { get; set; }
    public string Nome { get; set; } = "";
    public bool Ativo { get; set; } = true;
}
