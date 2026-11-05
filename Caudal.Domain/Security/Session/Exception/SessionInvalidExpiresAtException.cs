using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Session.Exception;

public class SessionInvalidExpiresAtException : DomainException
{
    public SessionInvalidExpiresAtException(string message) : base(message){}
}
