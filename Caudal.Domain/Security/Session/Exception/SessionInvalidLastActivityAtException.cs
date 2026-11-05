using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Session.Exception;

public class SessionInvalidLastActivityAtException : DomainException
{
    public SessionInvalidLastActivityAtException(string message) : base(message){}
}
