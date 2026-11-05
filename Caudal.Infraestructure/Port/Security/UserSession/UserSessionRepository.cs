using Caudal.Domain.Security.Session.Adapter;
using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure.Port.Security;

internal sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly IApplicationDbContext _dbcontext;
    public UserSessionRepository(IApplicationDbContext dbContext)
    {
        _dbcontext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    public async Task AddAsync(UserSession session, CancellationToken cancellationToken)
    {
        if (_dbcontext.UserSessions is null)
            throw new ArgumentNullException(nameof(_dbcontext.UserSessions));

        await _dbcontext.UserSessions.AddAsync(session, cancellationToken);
    }
    public void Remove(UserSession session)
    {
        if (_dbcontext.UserSessions is null)
            throw new ArgumentNullException(nameof(_dbcontext.UserSessions));

        _dbcontext.UserSessions.Remove(session);
    }
    public async Task RemoveExpiredByUserAsync(UserId userId, DateTime now, CancellationToken cancellationToken)
    {
        if (_dbcontext.UserSessions is null)
            throw new ArgumentNullException(nameof(_dbcontext.UserSessions));

        // Se cargan en lugar de borrarlas directo: así se guardan junto con lo demás en SaveChanges.
        var expired = await _dbcontext.UserSessions
            .Where(x => x.UserId == userId && x.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        _dbcontext.UserSessions.RemoveRange(expired);
    }
    public async Task<UserSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken)
    {
        if (_dbcontext.UserSessions is null)
            throw new ArgumentNullException(nameof(_dbcontext.UserSessions));

        return await _dbcontext.UserSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
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
