using System.Security.Cryptography;
using System.Text;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Infraestructure.Port.Security;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 600_000;
    private const int HashSize = 32;
    private const char Separator = '.';

    private readonly ISaltCodeGenerator _saltCodeGenerator;

    public PasswordHasher(ISaltCodeGenerator saltCodeGenerator)
    {
        _saltCodeGenerator = saltCodeGenerator ?? throw new ArgumentNullException(nameof(saltCodeGenerator));
    }

    public (string Hash, SaltCode Salt) Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = _saltCodeGenerator.Generate();
        var hash = DeriveHash(password, salt, Iterations);

        return ($"{Iterations}{Separator}{Convert.ToBase64String(hash)}", salt);
    }

    public bool Verify(string password, string hash, SaltCode salt)
    {
        var parts = hash.Split(Separator);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var iterations) || iterations <= 0)
            return false;

        var expectedHash = new byte[HashSize];
        if (!Convert.TryFromBase64String(parts[1], expectedHash, out var bytesWritten) || bytesWritten != HashSize)
            return false;

        return CryptographicOperations.FixedTimeEquals(expectedHash, DeriveHash(password, salt, iterations));
    }

    private static byte[] DeriveHash(string password, SaltCode salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            Encoding.UTF8.GetBytes(salt.Value!),
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }
}
