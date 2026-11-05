using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Session.Exception;

public class SessionInvalidIdException : DomainException
{
    public SessionInvalidIdException(string Message) : base(Message){}
}
