namespace Caudal.Domain.Security.Exception;

using Caudal.Domain.Shared.ValueObjects;

public class UserInvalidSaltCodeException : DomainException
{
    public UserInvalidSaltCodeException(string message) : base(message){}
}
