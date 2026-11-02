namespace Caudal.Domain.Shared.ValueObjects;

public class DomainException :  System.Exception
{
    public DomainException(string message) : base(message){}
}
