namespace Caudal.Domain.Security.Exception;

using Caudal.Domain.Shared.ValueObjects;

public class UserInvalidEmailFormatException : DomainException
{
    public UserInvalidEmailFormatException(string message) : base(message){}
}
