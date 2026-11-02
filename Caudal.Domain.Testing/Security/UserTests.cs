using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Enums;
using Caudal.Domain.Security.Exception;
using Caudal.Domain.Security.ValueObjects;
using Moq;
using Xunit;

namespace Caudal.Domain.Testing.Security;

public class UserTests
{
    private static readonly DateTime UtcDate = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    private static User CreateUser(UserStatus status = UserStatus.Active)
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            "Brian Gomez",
            Email.Create("brian@caudal.com"),
            "hashed-password",
            new SaltCode("abc123"),
            UtcDate);

        switch (status)
        {
            case UserStatus.Suspended:
                user.Suspend();
                break;
            case UserStatus.Locked:
                user.Lock();
                break;
            case UserStatus.Deleted:
                user.Delete();
                break;
        }

        return user;
    }

    // Bueno: el flujo esperado se completa correctamente.

    [Fact]
    public void Constructor_WithValidData_CreatesActiveUser()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var email = Email.Create("brian@caudal.com");
        var saltCode = new SaltCode("abc123");

        // Act
        var user = new User(userId, "Brian Gomez", email, "hashed-password", saltCode, UtcDate);

        // Assert
        Assert.Equal(userId, user.UserId);
        Assert.Equal("Brian Gomez", user.FullName);
        Assert.Equal(email, user.Email);
        Assert.Equal("hashed-password", user.Password);
        Assert.Equal(saltCode, user.SaltCode);
        Assert.Equal(UtcDate, user.AccountCreatedAt);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Null(user.EmailVerifiedAt);
        Assert.Null(user.AccountLockedUntil);
    }

    [Fact]
    public void UpdateEmailVerifiedAt_WithUtcDate_SetsDate()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.UpdateEmailVerifiedAt(UtcDate);

        // Assert
        Assert.Equal(UtcDate, user.EmailVerifiedAt);
    }

    [Fact]
    public void UpdateAccountLockedUntil_WithUtcDate_SetsDate()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.UpdateAccountLockedUntil(UtcDate);

        // Assert
        Assert.Equal(UtcDate, user.AccountLockedUntil);
    }

    [Fact]
    public void Activate_WhenSuspended_SetsActiveStatus()
    {
        // Arrange
        var user = CreateUser(UserStatus.Suspended);

        // Act
        user.Activate();

        // Assert
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Suspend_WhenActive_SetsSuspendedStatus()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.Suspend();

        // Assert
        Assert.Equal(UserStatus.Suspended, user.Status);
    }

    [Fact]
    public void Lock_WhenActive_SetsLockedStatus()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.Lock();

        // Assert
        Assert.Equal(UserStatus.Locked, user.Status);
    }

    [Fact]
    public void Delete_WhenActive_SetsDeletedStatus()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.Delete();

        // Assert
        Assert.Equal(UserStatus.Deleted, user.Status);
    }

    [Fact]
    public void EmailCreate_WithSpacesAndUpperCase_NormalizesValue()
    {
        // Arrange
        const string value = "  Brian@Caudal.COM ";

        // Act
        var email = Email.Create(value);

        // Assert
        Assert.Equal("brian@caudal.com", email.Value);
    }

    [Fact]
    public async Task GetByEmailAsync_WithEquivalentEmail_ReturnsUser()
    {
        // Arrange
        var user = CreateUser();
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByEmailAsync(Email.Create("brian@caudal.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await repository.Object.GetByEmailAsync(Email.Create("BRIAN@caudal.com"), CancellationToken.None);

        // Assert
        Assert.Same(user, result);
    }

    // Malo: el dominio rechaza datos u operaciones que violan sus reglas.

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_WithNonUtcDate_ThrowsException(DateTimeKind kind)
    {
        // Arrange
        var accountCreatedAt = DateTime.SpecifyKind(UtcDate, kind);

        // Act
        var act = () => new User(
            new UserId(Guid.NewGuid()),
            "Brian Gomez",
            Email.Create("brian@caudal.com"),
            "hashed-password",
            new SaltCode("abc123"),
            accountCreatedAt);

        // Assert
        Assert.Throws<UserInvalidAccountCreateAtException>(act);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void UpdateEmailVerifiedAt_WithNonUtcDate_ThrowsExceptionAndKeepsValue(DateTimeKind kind)
    {
        // Arrange
        var user = CreateUser();
        var emailVerifiedAt = DateTime.SpecifyKind(UtcDate, kind);

        // Act
        var act = () => user.UpdateEmailVerifiedAt(emailVerifiedAt);

        // Assert
        Assert.Throws<UserInvalidEmailVerifiedAtException>(act);
        Assert.Null(user.EmailVerifiedAt);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void UpdateAccountLockedUntil_WithNonUtcDate_ThrowsExceptionAndKeepsValue(DateTimeKind kind)
    {
        // Arrange
        var user = CreateUser();
        var accountLockedUntil = DateTime.SpecifyKind(UtcDate, kind);

        // Act
        var act = () => user.UpdateAccountLockedUntil(accountLockedUntil);

        // Assert
        Assert.Throws<UserInvalidAccountLockedUntilException>(act);
        Assert.Null(user.AccountLockedUntil);
    }

    [Fact]
    public void Suspend_WhenDeleted_ThrowsExceptionAndKeepsStatus()
    {
        // Arrange
        var user = CreateUser(UserStatus.Deleted);

        // Act
        var act = () => user.Suspend();

        // Assert
        Assert.Throws<UserInvalidStatusChangeException>(act);
        Assert.Equal(UserStatus.Deleted, user.Status);
    }

    [Fact]
    public void Lock_WhenDeleted_ThrowsExceptionAndKeepsStatus()
    {
        // Arrange
        var user = CreateUser(UserStatus.Deleted);

        // Act
        var act = () => user.Lock();

        // Assert
        Assert.Throws<UserInvalidStatusChangeException>(act);
        Assert.Equal(UserStatus.Deleted, user.Status);
    }

    [Fact]
    public void Delete_WhenSuspended_ThrowsExceptionAndKeepsStatus()
    {
        // Arrange
        var user = CreateUser(UserStatus.Suspended);

        // Act
        var act = () => user.Delete();

        // Assert
        Assert.Throws<UserInvalidStatusChangeException>(act);
        Assert.Equal(UserStatus.Suspended, user.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("brian.caudal.com")]
    public void EmailCreate_WithInvalidValue_ThrowsException(string? value)
    {
        // Arrange
        // value proviene de InlineData

        // Act
        var act = () => Email.Create(value!);

        // Assert
        Assert.Throws<UserInvalidEmailFormatException>(act);
    }

    [Fact]
    public void EmailCreate_WithMoreThan100Characters_ThrowsException()
    {
        // Arrange
        var value = new string('a', 92) + "@caudal.com";

        // Act
        var act = () => Email.Create(value);

        // Assert
        Assert.Throws<UserInvalidEmailFormatException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901")]
    public void SaltCode_WithInvalidValue_ThrowsException(string? value)
    {
        // Arrange
        // value proviene de InlineData

        // Act
        var act = () => new SaltCode(value!);

        // Assert
        Assert.Throws<UserInvalidSaltCodeException>(act);
    }

    [Fact]
    public void UserId_WithEmptyGuid_ThrowsException()
    {
        // Arrange
        var value = Guid.Empty;

        // Act
        var act = () => new UserId(value);

        // Assert
        Assert.Throws<UserInvalidIdException>(act);
    }

    // Fallo: una dependencia externa falla de forma inesperada.

    [Fact]
    public async Task AddUserAsync_WhenRepositoryFails_PropagatesExceptionAndKeepsUserState()
    {
        // Arrange
        var user = CreateUser();
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.AddUserAsync(user, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timeout."));

        // Act
        var act = () => repository.Object.AddUserAsync(user, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(act);
        Assert.Equal(UserStatus.Active, user.Status);
        repository.Verify(r => r.AddUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
