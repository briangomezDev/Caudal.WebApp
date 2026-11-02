using Caudal.Domain.Security.Enums;
using Caudal.Domain.Security.Exception;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.Entity;

public class User : IAgregateRoot
{
    public UserId? UserId { get; private set; }
    public string? FullName { get; private set; }
    public Email? Email { get; private set; }
    public string? Password { get; private set; }
    public SaltCode? SaltCode { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime? AccountCreatedAt { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public DateTime? AccountLockedUntil { get; private set; }

    public User(UserId userId, string fullName, Email email, string password, SaltCode saltCode, DateTime accountCreatedAt)
    {
        if(accountCreatedAt.Kind != DateTimeKind.Utc)
            throw new UserInvalidAccountCreateAtException("Account created at must be in UTC format.");

        UserId = userId;
        FullName = fullName;
        Email = email;
        Password = password;
        SaltCode = saltCode;
        AccountCreatedAt = accountCreatedAt;
        Status = UserStatus.Active;
    }

    public void UpdateEmailVerifiedAt(DateTime emailVerifiedAt)
    {
        if (emailVerifiedAt.Kind != DateTimeKind.Utc)
            throw new UserInvalidEmailVerifiedAtException("Email verified at must be in UTC format.");

        EmailVerifiedAt = emailVerifiedAt;
    }
    public void UpdateAccountLockedUntil(DateTime accountLockedUntil)
    {
        if (accountLockedUntil.Kind != DateTimeKind.Utc)
            throw new UserInvalidAccountLockedUntilException("Account locked until must be in UTC format.");

        AccountLockedUntil = accountLockedUntil;
    }
    public void Activate()
    {
        Status = UserStatus.Active;
    }
    public void Suspend()
    {
        if (this.Status == UserStatus.Deleted)
            throw new UserInvalidStatusChangeException("Cannot suspend a deleted user.");

        Status = UserStatus.Suspended;
    }
    public void Lock()
    {
        if (this.Status == UserStatus.Deleted)
            throw new UserInvalidStatusChangeException("Cannot lock a deleted user.");

        Status = UserStatus.Locked;
    }
    public void Delete()
    {
        if (this.Status == UserStatus.Suspended)
            throw new UserInvalidStatusChangeException("Cannot delete a suspended user.");

        Status = UserStatus.Deleted;
    }
}
