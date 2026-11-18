using System.Globalization;

namespace BarberHub.Web.Infra;

/// <summary>Leitura tolerante de campos digitados (aceita "40,00" e "40.00").</summary>
public static class Formulario
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static decimal? Decimal(string? texto)
    {
        texto = texto?.Replace("R$", "").Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        var cultura = texto.Contains(',') ? PtBr : CultureInfo.InvariantCulture;
        return decimal.TryParse(texto, NumberStyles.Number, cultura, out var valor) ? valor : null;
    }

    public static DateOnly? Data(string? texto) =>
        DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public static TimeOnly? Hora(string? texto) =>
        TimeOnly.TryParseExact(texto, ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var h) ? h : null;

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    /// <summary>"Terça-feira, 24 de novembro": só a primeira letra maiúscula.</summary>
    public static string DiaPorExtenso(DateOnly dia)
    {
        var texto = dia.ToString("dddd, d 'de' MMMM", PtBr);
        return char.ToUpper(texto[0], PtBr) + texto[1..];
    }
}
