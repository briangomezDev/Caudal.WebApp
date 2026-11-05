using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Domain.Security.Session.Adapter;

public interface IUserSessionRepository
{
    Task AddAsync(UserSession session, CancellationToken cancellationToken);
    Task<UserSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken);
    Task<UserSession?> GetByUserIdAsync(UserId userId);
    Task<ICollection<UserSession>> GetByUserAndDeviceAsync(UserId userId, string device);
}
