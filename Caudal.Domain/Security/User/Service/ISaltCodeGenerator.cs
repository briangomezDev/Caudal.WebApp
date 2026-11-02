using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Domain.Security.Service;

public interface ISaltCodeGenerator
{
    SaltCode Generate();
}
