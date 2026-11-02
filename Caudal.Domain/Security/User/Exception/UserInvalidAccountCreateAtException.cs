using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Exception;

public class UserInvalidAccountCreateAtException : DomainException
{
    public UserInvalidAccountCreateAtException(string message) : base(message){}
}
