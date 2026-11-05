using Caudal.Domain.Security.Session.Exception;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.Session.ValueObjects;

public class SessionId : ValueObject
{
    public Guid Value { get; private set; }

    public SessionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new SessionInvalidIdException("SessionId cannot be empty.");

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
