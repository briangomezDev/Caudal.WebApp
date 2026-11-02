namespace Caudal.Domain.Security.Exception;

using Caudal.Domain.Security.ValueObjects;

public class UserInvalidStatusChangeException : DomainException
{
    public UserInvalidStatusChangeException(string message) : base(message){}
}
