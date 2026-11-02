namespace Caudal.Domain.Shared;

public abstract class ValueObject
{
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (left is null ^ right is null)
        {
            return false;
        }

        return left?.Equals(right) ?? true;
    }
    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
    {
        return !(EqualOperator(left, right));
    }
    protected abstract IEnumerable<object> GetEqualityComponents();
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
            {
                return HashCode.Combine(current, obj?.GetHashCode() ?? 0);
            });
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return EqualOperator(left, right);
    }
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return NotEqualOperator(left, right);   
    }
}
