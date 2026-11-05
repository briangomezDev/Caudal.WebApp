using Caudal.Domain.Security.Session.Adapter;
using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Infraestructure.Port.Security;

internal sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly IApplicationDbContext _dbcontext;
    public UserSessionRepository(IApplicationDbContext dbContext)
    {
        _dbcontext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    public async Task<UserSession?> GetByUserIdAsync(UserId userId)
    {
        throw new NotImplementedException();
    }
    public async Task<ICollection<UserSession>> GetByUserAndDeviceAsync(UserId userId, string device)
    {
        throw new NotImplementedException();
    }
}
