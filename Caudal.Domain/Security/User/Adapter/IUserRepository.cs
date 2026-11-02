using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Domain.Security.Adapter;

public interface IUserRepository
{
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(Email? email, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(Email? email, CancellationToken cancellationToken);
    Task<User?> GetByProviderAsync(int providerId, string providerUserId, CancellationToken cancellationToken);
}
