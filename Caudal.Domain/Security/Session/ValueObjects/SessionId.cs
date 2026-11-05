using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.Session.ValueObjects;

public class SessionId : ValueObject
{
    public string? Value { get; private set; }
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
    }
}
