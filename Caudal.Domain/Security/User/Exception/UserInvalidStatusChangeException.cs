namespace Caudal.Domain.Security.Exception;

using Caudal.Domain.Shared.ValueObjects;

public class UserInvalidStatusChangeException : DomainException
{
    public UserInvalidStatusChangeException(string message) : base(message){}
}
