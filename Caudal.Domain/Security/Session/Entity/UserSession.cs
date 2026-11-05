using Caudal.Domain.Security.Session.Exception;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.Session.Entity;

public class UserSession : IAgregateRoot
{
    public const int DeviceMaxLength = 100;

    public SessionId? SessionId { get; private set; }
    public UserId? UserId { get; private set; }
    public string? Device { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    private UserSession() { }
    public UserSession(SessionId sessionId, UserId userId, string device, DateTime createdAt, DateTime expiresAt)
    {
        if (createdAt.Kind != DateTimeKind.Utc)
            throw new SessionInvalidCreatedAtException("Session created at must be in UTC format");

        if (expiresAt.Kind != DateTimeKind.Utc || expiresAt <= createdAt)
            throw new SessionInvalidExpiresAtException("Session expires at must be in UTC format and later than its creation");

        if (string.IsNullOrWhiteSpace(device) || device.Length > DeviceMaxLength)
            throw new SessionInvalidDeviceException($"Session device is required and must be {DeviceMaxLength} characters or fewer");

        SessionId = sessionId;
        UserId = userId;
        Device = device;
        CreatedAt = createdAt;
        LastActivityAt = createdAt;
        ExpiresAt = expiresAt;
    }
    public bool IsActive(DateTime now) => ExpiresAt > now;
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
    /// <summary>
    /// Registra actividad y extiende la expiración conservando la duración con la que se creó la sesión.
    /// </summary>
    public void Renew(DateTime now)
    {
        var duration = ExpiresAt - (LastActivityAt ?? CreatedAt);

        UpdateLastActivityAt(now);
        UpdateExpiresAt(now + duration);
    }
    /// <summary>
    /// Termina la sesión desde fuera del navegador del usuario. La fila se borra en su siguiente login.
    /// </summary>
    public void Expire(DateTime now)
    {
        UpdateExpiresAt(now);
    }
}
