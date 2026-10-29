using MundoAwoo.Infrastructure.Security;

namespace MundoAwoo.Tests.Security;

public class BcryptPasswordHasherTests
{
    [Fact]
    public void ContrasenasLargasConPrefijoIgualNoSonEquivalentes()
    {
        var hasher = new BcryptPasswordHasher();
        string prefijo = new string('A', 80);
        string hash = hasher.Hash(prefijo + "-uno");

        Assert.True(hasher.Verify(prefijo + "-uno", hash));
        Assert.False(hasher.Verify(prefijo + "-dos", hash));
    }
}
