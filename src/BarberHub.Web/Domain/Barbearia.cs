namespace BarberHub.Web.Domain;

public enum Plano { Basico = 1, Pro = 2 }

/// <summary>Cliente do SaaS (tenant). Todos os dados do sistema pertencem a uma barbearia.</summary>
public class Barbearia
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public Plano Plano { get; set; }
    public DateOnly AssinaturaValidaAte { get; set; }
    public bool EmPeriodoGratis { get; set; }
}

/// <summary>Horário de funcionamento de um dia da semana. Dia sem registro = fechado.</summary>
public class HorarioFuncionamento
{
    public int Id { get; set; }
    public int BarbeariaId { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly Abre { get; set; }
    public TimeOnly Fecha { get; set; }
}
