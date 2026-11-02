using System.Security.Cryptography;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Infraestructure.Port.Security;

internal sealed class CryptographySaltCodeGenerator : ISaltCodeGenerator
{
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int Length = 10;

    public SaltCode Generate()
    {
        return new SaltCode(RandomNumberGenerator.GetString(Characters, Length));
    }
}
