using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Tests.Domain;

public class ColorHexTests
{
    [Fact]
    public void ColoresConElMismoValor_SonIgualesTrasNormalizar()
    {
        var primero = new ColorHex(" #aabbcc ");
        var segundo = new ColorHex("#AABBCC");

        Assert.Equal(primero, segundo);
        Assert.True(primero == segundo);
        Assert.Equal(primero.GetHashCode(), segundo.GetHashCode());
        Assert.Equal("#AABBCC", primero.Valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("FFFFFF")]
    [InlineData("#FFF")]
    [InlineData("#GG0000")]
    [InlineData("#FFFFFF00")]
    public void ColorInvalido_NoPuedeCrearse(string? valor)
    {
        Assert.Throws<ArgumentException>(() => new ColorHex(valor!));
    }
}
