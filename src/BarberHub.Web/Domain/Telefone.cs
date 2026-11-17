namespace BarberHub.Web.Domain;

/// <summary>Regras do celular do cliente: é por ele que a barbearia confirma o horário no WhatsApp.</summary>
public static class Telefone
{
    private static readonly HashSet<int> DddsValidos =
    [
        11, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 24, 27, 28,
        31, 32, 33, 34, 35, 37, 38, 41, 42, 43, 44, 45, 46, 47, 48, 49,
        51, 53, 54, 55, 61, 62, 63, 64, 65, 66, 67, 68, 69,
        71, 73, 74, 75, 77, 79, 81, 82, 83, 84, 85, 86, 87, 88, 89,
        91, 92, 93, 94, 95, 96, 97, 98, 99
    ];

    public const string MensagemInvalido = "Informe um celular válido com DDD, ex.: (11) 98765-4321.";

    /// <summary>Remove máscara e +55 e devolve somente DDD + 9 dígitos (ex.: 11987654321).</summary>
    public static Resultado<string> Normalizar(string? digitado)
    {
        var digitos = new string((digitado ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length == 13 && digitos.StartsWith("55"))
            digitos = digitos[2..];

        var valido = digitos.Length == 11
                     && DddsValidos.Contains(int.Parse(digitos[..2]))
                     && digitos[2] == '9';

        return valido ? Resultado<string>.Ok(digitos) : Resultado<string>.Falha(MensagemInvalido);
    }

    public static string Formatar(string normalizado) => throw new NotImplementedException();
}
