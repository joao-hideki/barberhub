namespace BarberHub.Web.Domain;

public record InfoPlano(Plano Plano, string Nome, decimal PrecoMensal, int LimiteBarbeiros);

/// <summary>Tabela de preços do SaaS.</summary>
public static class Planos
{
    public static readonly InfoPlano Basico = new(Plano.Basico, "Básico", 49.90m, 1);
    public static readonly InfoPlano Pro = new(Plano.Pro, "Pro", 99.90m, 5);

    public static IReadOnlyList<InfoPlano> Todos { get; } = [Basico, Pro];

    public static InfoPlano Info(Plano plano) => Todos.Single(p => p.Plano == plano);
}
