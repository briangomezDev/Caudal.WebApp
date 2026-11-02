using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Exception;

public class UserInvalidAccountLockedUntilException : DomainException
{
    public UserInvalidAccountLockedUntilException(string message) : base(message){}
}
