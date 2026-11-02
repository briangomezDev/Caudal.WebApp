using Caudal.Domain.Security.Exception;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.ValueObjects;

public class Email : ValueObject
{
    public string? Value { get; private set; }
    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new UserInvalidEmailFormatException("Email cannot be null or empty.");

        if (!value.Contains("@"))
            throw new UserInvalidEmailFormatException("Email must contain '@' symbol.");
            
        if(value.Length > 100)
            throw new UserInvalidEmailFormatException("Email must be no more than 100 characters long.");

        Value = value.Trim().ToLower();
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
