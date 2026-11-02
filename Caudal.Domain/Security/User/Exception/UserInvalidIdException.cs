using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Exception;

public class UserInvalidIdException : DomainException
{
    public UserInvalidIdException(string message) : base(message){}
}
