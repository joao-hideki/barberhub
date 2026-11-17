using BarberHub.Web.Domain;

namespace BarberHub.Tests;

public class TelefoneTests
{
    [Theory]
    [InlineData("(11) 98765-4321")]
    [InlineData("11987654321")]
    [InlineData("11 9 8765 4321")]
    [InlineData("+55 (11) 98765-4321")]
    public void Celular_em_qualquer_formato_e_normalizado_para_DDD_mais_numero(string digitado)
    {
        var resultado = Telefone.Normalizar(digitado);

        Assert.True(resultado.Sucesso);
        Assert.Equal("11987654321", resultado.Valor);
    }
}
