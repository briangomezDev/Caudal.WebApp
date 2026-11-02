namespace Caudal.Domain.Security.Exception;

using Caudal.Domain.Security.ValueObjects;

public class UserInvalidEmailFormatException : DomainException
{
    public UserInvalidEmailFormatException(string message) : base(message){}
}
