namespace Caudal.Domain.Security.ValueObjects;

public class DomainException :  System.Exception
{
    public DomainException(string message) : base(message){}
}
