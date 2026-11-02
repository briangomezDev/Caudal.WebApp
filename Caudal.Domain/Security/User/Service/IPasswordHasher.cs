using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Domain.Security.Service;

public interface IPasswordHasher
{
    (string Hash, SaltCode Salt) Hash(string password);
    bool Verify(string password, string hash, SaltCode salt);
}
