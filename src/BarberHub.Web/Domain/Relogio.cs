namespace BarberHub.Web.Domain;

/// <summary>Converte o relógio do sistema para o horário de Brasília.</summary>
public static class Relogio
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTime AgoraLocal(this TimeProvider relogio) =>
        TimeZoneInfo.ConvertTime(relogio.GetUtcNow(), Fuso).DateTime;
}
