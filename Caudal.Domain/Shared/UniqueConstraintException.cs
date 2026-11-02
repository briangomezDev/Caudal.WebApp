namespace Caudal.Domain.Shared.ValueObjects;

public class UniqueConstraintException : DomainException
{
    public string? ConstraintName { get; }

    public UniqueConstraintException(string? constraintName, string message) : base(message)
    {
        ConstraintName = constraintName;
    }
}
