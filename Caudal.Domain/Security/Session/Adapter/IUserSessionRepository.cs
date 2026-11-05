using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.ValueObjects;

namespace Caudal.Domain.Security.Session.Adapter;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByUserIdAsync(UserId userId);
    Task<ICollection<UserSession>> GetByUserAndDeviceAsync(UserId userId, string device);
}
