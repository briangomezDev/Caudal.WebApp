using Caudal.Domain.Security.Exception;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.ValueObjects;

public class UserId : ValueObject
{
    public Guid Value { get; private set; }

    public UserId(Guid value)
    {
        if (value == Guid.Empty)
            throw new UserInvalidIdException("UserId cannot be empty.");

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

