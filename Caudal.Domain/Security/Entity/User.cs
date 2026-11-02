using Caudal.Domain.Security.Enums;
using Caudal.Domain.Security.Exception;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared;

namespace Caudal.Domain.Security.Entity;

public class User : IAgregateRoot
{
    public string? FullName { get; private set; }
    public Email? Email { get; private set; }
    public string? Password { get; private set; }
    public SaltCode? SaltCode { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public DateTime? AccountLockedUntil { get; private set; }

    public User(string fullName, Email email, string password, SaltCode saltCode)
    {
        FullName = fullName;
        Email = email;
        Password = password;
        SaltCode = saltCode;
        Status = UserStatus.Active;
    }

    public void UpdateEmailVerifiedAt(DateTime emailVerifiedAt)
    {
        EmailVerifiedAt = emailVerifiedAt;
    }
    public void UpdateAccountLockedUntil(DateTime accountLockedUntil)
    {
        AccountLockedUntil = accountLockedUntil;
    }
    public void UpdateStatus(UserStatus status)
    {
        if(this.Status == UserStatus.Deleted)
            throw new UserInvalidStatusChangeException("Cannot change status of a deleted user.");

        Status = status;
    }
}
