using Caudal.Application.Services.AppUser.Logout;
using Caudal.Application.Services.AppUser.ValidateSession;
using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Session.Adapter;
using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caudal.Application.Testing.Services.AppUser.ValidateSession;

public class UserSessionLifecycleTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserSessionRepository> _sessions = new();
    private readonly Mock<IUserRepository> _users = new();

    private UserSessionValidationUseCase CreateValidation() =>
        new(NullLogger<UserSessionValidationUseCase>.Instance, _unitOfWork.Object, _sessions.Object, _users.Object);

    private UserLogoutUseCase CreateLogout() =>
        new(NullLogger<UserLogoutUseCase>.Instance, _unitOfWork.Object, _sessions.Object);

    private static User CreateUser() => new(
        new UserId(Guid.NewGuid()),
        "Brian Gomez",
        Email.Create("brian@caudal.com"),
        "hashed-password",
        new SaltCode("abc123"),
        DateTime.UtcNow);

    private UserSession SessionExists(TimeSpan createdAgo, TimeSpan duration, User? user = null)
    {
        user ??= CreateUser();
        var createdAt = DateTime.UtcNow - createdAgo;
        var session = new UserSession(new SessionId(Guid.NewGuid()), user.UserId!, "Firefox en Linux", createdAt, createdAt + duration);
        _sessions
            .Setup(s => s.GetByIdAsync(It.IsAny<SessionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _users
            .Setup(u => u.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return session;
    }

    private void VerifySaved(Times times) =>
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), times);

    // Bueno: una sesión vigente sigue viva, y solo se escribe cuando toca renovarla.

    [Fact]
    public async Task ValidateAsync_WithRecentActivity_IsValidAndDoesNotWrite()
    {
        // Arrange
        var session = SessionExists(TimeSpan.FromSeconds(10), TimeSpan.FromHours(8));

        // Act
        var result = await CreateValidation().ValidateAsync(session.SessionId!.Value, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.False(result.Value.Renewed);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task ValidateAsync_AfterRenewalInterval_ExtendsExpirationWithSameDuration()
    {
        // Arrange
        var session = SessionExists(TimeSpan.FromHours(1), TimeSpan.FromHours(8));
        var before = DateTime.UtcNow;

        // Act
        var result = await CreateValidation().ValidateAsync(session.SessionId!.Value, CancellationToken.None);

        // Assert
        Assert.True(result.Value.Renewed);
        Assert.True(result.Value.ExpiresAt >= before.AddHours(8));
        Assert.Equal(TimeSpan.FromHours(8), session.ExpiresAt - session.LastActivityAt);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task LogoutAsync_WithActiveSession_RemovesIt()
    {
        // Arrange
        var session = SessionExists(TimeSpan.FromMinutes(5), TimeSpan.FromHours(8));

        // Act
        var result = await CreateLogout().LogoutAsync(session.SessionId!.Value, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _sessions.Verify(s => s.Remove(session), Times.Once);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task ValidateAsync_DuringTemporaryLockout_KeepsSessionOpen()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateAccountLockedUntil(DateTime.UtcNow.AddMinutes(10));
        var session = SessionExists(TimeSpan.FromSeconds(10), TimeSpan.FromHours(8), user);

        // Act
        var result = await CreateValidation().ValidateAsync(session.SessionId!.Value, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.True(session.IsActive(DateTime.UtcNow));
    }

    // Malo: sin sesión vigente, o con el usuario fuera de servicio, no hay acceso.

    [Fact]
    public async Task ValidateAsync_WithUnknownSession_IsInvalid()
    {
        // Act
        var result = await CreateValidation().ValidateAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyId_IsInvalidWithoutQuerying()
    {
        // Act
        var result = await CreateValidation().ValidateAsync(Guid.Empty, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        _sessions.Verify(s => s.GetByIdAsync(It.IsAny<SessionId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WithExpiredSession_IsInvalid()
    {
        // Arrange
        var session = SessionExists(TimeSpan.FromHours(9), TimeSpan.FromHours(8));

        // Act
        var result = await CreateValidation().ValidateAsync(session.SessionId!.Value, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task ValidateAsync_WithSuspendedUser_EndsSessionAndIsInvalid()
    {
        // Arrange
        var user = CreateUser();
        user.Suspend();
        var session = SessionExists(TimeSpan.FromMinutes(5), TimeSpan.FromHours(8), user);

        // Act
        var result = await CreateValidation().ValidateAsync(session.SessionId!.Value, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        Assert.False(session.IsActive(DateTime.UtcNow));
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task ValidateAsync_WhenRepositoryFails_ReturnsFailureNotInvalid()
    {
        // Arrange
        _sessions
            .Setup(s => s.GetByIdAsync(It.IsAny<SessionId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        // Act
        var result = await CreateValidation().ValidateAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
    }

    [Fact]
    public async Task LogoutAsync_WithUnknownSession_IsNotAnErrorAndDoesNotWrite()
    {
        // Act
        var result = await CreateLogout().LogoutAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        VerifySaved(Times.Never());
    }
}
