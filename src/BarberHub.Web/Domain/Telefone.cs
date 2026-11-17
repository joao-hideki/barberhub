namespace BarberHub.Web.Domain;

public static class Telefone
{
    public static Resultado<string> Normalizar(string? digitado)
    {
        var digitos = new string((digitado ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length == 13 && digitos.StartsWith("55"))
            digitos = digitos[2..];
        return Resultado<string>.Ok(digitos);
    }
}
