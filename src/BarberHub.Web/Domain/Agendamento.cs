namespace BarberHub.Web.Domain;

/// <summary>Cliente final da barbearia. Sem senha: é identificado pelo celular.</summary>
public class Cliente
{
    public int Id { get; set; }
    public int BarbeariaId { get; set; }
    public string Nome { get; set; } = "";
    public string Telefone { get; set; } = "";
}

public enum StatusAgendamento { Agendado = 1, Confirmado = 2, Concluido = 3, Cancelado = 4 }

public class Agendamento
{
    public int Id { get; set; }
    public int BarbeariaId { get; set; }
    public int BarbeiroId { get; set; }
    public Barbeiro Barbeiro { get; set; } = null!;
    public int ServicoId { get; set; }
    public Servico Servico { get; set; } = null!;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    /// <summary>O horário fica bloqueado no intervalo [Inicio, Fim).</summary>
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }

    /// <summary>Preço do serviço no momento do agendamento (não muda se o catálogo mudar).</summary>
    public decimal Preco { get; set; }
    public StatusAgendamento Status { get; set; } = StatusAgendamento.Agendado;
}

public static class StatusAgendamentoExtensions
{
    public static string Rotulo(this StatusAgendamento status) => status switch
    {
        StatusAgendamento.Agendado => "Agendado",
        StatusAgendamento.Confirmado => "Confirmado",
        StatusAgendamento.Concluido => "Concluído",
        StatusAgendamento.Cancelado => "Cancelado",
        _ => status.ToString()
    };
}
