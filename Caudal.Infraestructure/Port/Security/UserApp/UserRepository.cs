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

    public async Task<bool> ExistsByEmailAsync(Email? email, CancellationToken cancellationToken)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        return await _dbcontext.Users.AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email? email, CancellationToken cancellationToken)
    {
        if (_dbcontext.Users is null)
            throw new ArgumentNullException(nameof(_dbcontext.Users));

        return await _dbcontext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<User?> GetByProviderAsync(int providerId, string providerUserId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
