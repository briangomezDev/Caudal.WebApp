using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Domain.Security.Service;

public sealed record AccountConfirmationClaim(UserId UserId, Email Email);

public interface IAccountConfirmationTokens
{
    string Create(UserId userId, Email email);
    bool TryRead(string token, out AccountConfirmationClaim? claim);
}
