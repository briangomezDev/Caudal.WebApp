using System.Security.Cryptography;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared.ValueObjects;
using Microsoft.AspNetCore.DataProtection;

namespace Caudal.Infraestructure.Port.Security;

// El enlace firmado lleva su propia fecha límite: no hace falta tabla de tokens ni job que los limpie.
internal sealed class DataProtectionAccountConfirmationTokens : IAccountConfirmationTokens
{
    internal const string Purpose = "Caudal.AccountConfirmation.v1";
    private const char Separator = '|';

    private readonly ITimeLimitedDataProtector _protector;

    public DataProtectionAccountConfirmationTokens(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _protector = provider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
    }

    public string Create(UserId userId, Email email)
    {
        return _protector.Protect($"{userId.Value}{Separator}{email.Value}", User.ConfirmationWindow);
    }

    public bool TryRead(string token, out AccountConfirmationClaim? claim)
    {
        claim = null;

        try
        {
            var parts = _protector.Unprotect(token).Split(Separator, 2);
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
                return false;

            claim = new AccountConfirmationClaim(new UserId(userId), Email.Create(parts[1]));
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or DomainException)
        {
            return false;
        }
    }
}
