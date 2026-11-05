using Caudal.Domain.Shared.ValueObjects;

namespace Caudal.Domain.Security.Session.Exception;

public class SessionInvalidDeviceException : DomainException
{
    public SessionInvalidDeviceException(string Message) : base(Message){}
}
