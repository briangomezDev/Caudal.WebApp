using Caudal.Domain.Security.Exception;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.ValueObjects;

public class Email : ValueObject
{
    public string? Value { get; private set; }
    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidEmailFormatException("Email cannot be null or empty.");

        if (!value.Contains("@"))
            throw new InvalidEmailFormatException("Email must contain '@' symbol.");

        Value = value;
    }
    public static Email Create(string value)
    {
        return new Email(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
    }
}
