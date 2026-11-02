using Caudal.Domain.Security.Exception;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.ValueObjects;

public class SaltCode : ValueObject
{
    public string? Value { get; private set; }
    public SaltCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
          throw new UserInvalidSaltCodeException("Salt code cannot be null or empty.");

        if (value.Length < 6)
            throw new UserInvalidSaltCodeException("Salt code must be at least 6 characters long.");

        Value = value;
    }
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
    }
}
