using Domain.Exceptions;
using Domain.VOs;

namespace Domain.Tests;

public class ValueObjectTests
{
    [Theory]
    [InlineData("11987654321")]
    [InlineData("(11) 98765-4321")]
    public void CelularVO_DeveAceitarNumeroComOnzeDigitos(string numero)
    {
        var celular = new CelularVO(numero);

        Assert.Equal(numero, celular.Numero);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1198765432")]
    [InlineData("119876543210")]
    public void CelularVO_DeveRejeitarNumeroInvalido(string numero)
    {
        Assert.Throws<DomainException>(() => new CelularVO(numero));
    }

    [Theory]
    [InlineData("529.982.247-25", "52998224725", "CPF")]
    [InlineData("111.444.777-35", "11144477735", "CPF")]
    [InlineData("11.222.333/0001-81", "11222333000181", "CNPJ")]
    public void DocumentoVO_DeveNormalizarDocumentoValido(string entrada, string numero, string tipo)
    {
        var documento = new DocumentoVO(entrada);

        Assert.Equal(numero, documento.Numero);
        Assert.Equal(tipo, documento.Tipo);
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-24")]
    [InlineData("11.222.333/0001-80")]
    [InlineData("11.111.111/1111-11")]
    [InlineData("   ")]
    [InlineData("123")]
    public void DocumentoVO_DeveRejeitarDocumentoInvalido(string numero)
    {
        Assert.Throws<DomainException>(() => new DocumentoVO(numero));
    }

    [Theory]
    [InlineData("cliente@example.com")]
    [InlineData("nome.sobrenome+teste@example.com")]
    public void EmailVO_DeveAceitarEnderecoValido(string endereco)
    {
        var email = new EmailVO(endereco);

        Assert.Equal(endereco, email.Endereco);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("email-invalido")]
    public void EmailVO_DeveRejeitarEnderecoInvalido(string endereco)
    {
        Assert.Throws<DomainException>(() => new EmailVO(endereco));
    }

    [Theory]
    [InlineData("abc-1234", "ABC-1234")]
    [InlineData("abc1d23", "ABC1D23")]
    [InlineData(" ABC-1234 ", "ABC-1234")]
    public void PlacaVO_DeveNormalizarPlacaValida(string entrada, string valor)
    {
        var placa = new PlacaVO(entrada);

        Assert.Equal(valor, placa.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB-1234")]
    [InlineData("ABC1234")]
    [InlineData("ABC1D2")]
    public void PlacaVO_DeveRejeitarPlacaInvalida(string placa)
    {
        Assert.Throws<DomainException>(() => new PlacaVO(placa));
    }

    [Fact]
    public void PlacaVO_DeveCompararPorValor()
    {
        var primeira = new PlacaVO("abc-1234");
        var igual = new PlacaVO("ABC-1234");
        var diferente = new PlacaVO("DEF-5678");

        Assert.True(primeira.Equals(igual));
        Assert.Equal(primeira.GetHashCode(), igual.GetHashCode());
        Assert.False(primeira.Equals(diferente));
        Assert.False(primeira.Equals(null));
        Assert.False(primeira.Equals("ABC-1234"));
    }

    [Fact]
    public void DomainException_DevePreservarMensagem()
    {
        var exception = new DomainException("Erro de domínio");

        Assert.Equal("Erro de domínio", exception.Message);
    }
}
