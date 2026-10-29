using MundoAwoo.Application.Security;

namespace MundoAwoo.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    // Prehash evita que BCrypt ignore los bytes posteriores a su límite de 72 bytes.
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11, enhancedEntropy: true);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash, enhancedEntropy: true);
}
