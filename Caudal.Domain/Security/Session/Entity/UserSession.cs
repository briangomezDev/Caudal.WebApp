using Caudal.Domain.Security.Session.Exception;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.Session.Entity;

public class UserSession : IAgregateRoot
{
    public SessionId? SessionId { get; private set; }
    public UserId? UserId { get; set; }
    public string? Device { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    private UserSession() { }
    public UserSession(SessionId sessionId, UserId userId, string device, DateTime createdAt)
    {
        if (createdAt.Kind != DateTimeKind.Utc)
            throw new SessionInvalidCreatedAtException("Session created at must be in UTC format");

        SessionId = sessionId;
        UserId = userId;
        CreatedAt = createdAt;
    }
    public void UpdateLastActivityAt(DateTime lastActivityAt)
    {
        if (lastActivityAt.Kind != DateTimeKind.Utc)
            throw new SessionInvalidLastActivityAtException("Session last activity at must be in UTC format");

        LastActivityAt = lastActivityAt;
    }
    public void UpdateExpiresAt(DateTime expiresAt)
    {
        if (expiresAt.Kind != DateTimeKind.Utc)
            throw new SessionInvalidExpiresAtException("Session expires at must be in UTC format");

        ExpiresAt = expiresAt;
    }
}
