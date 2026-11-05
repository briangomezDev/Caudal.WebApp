using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Session.Exception;

public class SessionInvalidCreatedAtException : DomainException
{
    public SessionInvalidCreatedAtException(string Message) : base(Message){}
}
