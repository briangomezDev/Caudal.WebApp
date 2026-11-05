using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure.Port.Security;

internal class UserRepository : IUserRepository
{
    private readonly IApplicationDbContext _dbcontext;
    public UserRepository(IApplicationDbContext dbContext)
    {
        _dbcontext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    public async Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        await _dbcontext.Users.AddAsync(user, cancellationToken);
    }

    public void RemoveUser(User user)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        _dbcontext.Users.Remove(user);
    }

    public async Task<bool> ExistsByEmailAsync(Email? email, CancellationToken cancellationToken)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        return await _dbcontext.Users.AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        return await _dbcontext.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email? email, CancellationToken cancellationToken)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        return await _dbcontext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public async Task RegisterFailedLoginAsync(UserId userId, DateTime now, CancellationToken cancellationToken)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        DateTime? lockedUntil = now + User.LockoutDuration;

        // Un solo UPDATE: dos fallos simultáneos cuentan como dos. Un bloqueo ya cumplido empieza la cuenta de nuevo.
        await _dbcontext.Users
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(
                    x => x.FailedLoginCount,
                    x => x.AccountLockedUntil != null && x.AccountLockedUntil <= now
                        ? (short)1
                        : (short)(x.FailedLoginCount + 1))
                .SetProperty(
                    x => x.AccountLockedUntil,
                    x => x.AccountLockedUntil != null && x.AccountLockedUntil <= now
                        ? null
                        : x.FailedLoginCount + 1 >= User.MaxFailedLogins
                            ? lockedUntil
                            : x.AccountLockedUntil),
                cancellationToken);
    }

    public async Task<User?> GetByProviderAsync(int providerId, string providerUserId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
