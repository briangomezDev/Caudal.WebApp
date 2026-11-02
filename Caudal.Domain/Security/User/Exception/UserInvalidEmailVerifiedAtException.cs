using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Exception;

public class UserInvalidEmailVerifiedAtException : DomainException
{
    public UserInvalidEmailVerifiedAtException(string message) : base(message){}
}
