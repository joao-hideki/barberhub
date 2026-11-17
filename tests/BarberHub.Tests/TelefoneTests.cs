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

public class TelefoneInvalidoTests
{
    [Theory]
    [InlineData("")]
    [InlineData("98765-4321")]        // sem DDD
    [InlineData("(20) 98765-4321")]   // DDD 20 não existe
    [InlineData("(11) 3265-4321")]    // fixo: sem o 9, não recebe WhatsApp
    [InlineData("(11) 98765-432")]    // faltando dígito
    [InlineData("abc")]
    public void Telefone_invalido_e_recusado_com_mensagem(string digitado)
    {
        var resultado = Telefone.Normalizar(digitado);

        Assert.False(resultado.Sucesso);
        Assert.Equal("Informe um celular válido com DDD, ex.: (11) 98765-4321.", resultado.Erro);
    }
}
