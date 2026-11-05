using Caudal.Application.Services.AppUser.Login;
using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.Session.Adapter;
using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Caudal.Application.Testing.Services.AppUser.Login;

public class UserLoginUseCaseTests
{
    private static readonly LoginRequest ValidRequest = new("brian@caudal.com", "Correct-password1", "Firefox en Linux", false);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserSessionRepository> _sessions = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private UserLoginUseCase CreateUseCase() => new(
        NullLogger<UserLoginUseCase>.Instance,
        _unitOfWork.Object,
        _users.Object,
        _sessions.Object,
        new LoginRequestValidator(),
        _passwordHasher.Object);

    private static User CreateUser() => new(
        new UserId(Guid.NewGuid()),
        "Brian Gomez",
        Email.Create("brian@caudal.com"),
        "hashed-password",
        new SaltCode("abc123"),
        DateTime.UtcNow);

    private User UserExists(User? user = null, bool passwordMatches = true)
    {
        user ??= CreateUser();
        _users
            .Setup(u => u.GetByEmailAsync(It.IsAny<Email?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(h => h.Verify(ValidRequest.Password, "hashed-password", It.IsAny<SaltCode>()))
            .Returns(passwordMatches);
        return user;
    }

    private void VerifyNoSession()
    {
        _sessions.Verify(s => s.AddAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Bueno: las credenciales correctas registran la sesión.

    [Theory]
    [InlineData(false, 8)]
    [InlineData(true, 24 * 30)]
    public async Task LoginAsync_WithValidCredentials_SavesSessionWithDeviceAndExpiration(bool rememberMe, int expectedHours)
    {
        // Arrange
        var user = UserExists();
        UserSession? saved = null;
        _sessions
            .Setup(s => s.AddAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
            .Callback<UserSession, CancellationToken>((session, _) => saved = session);

        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest with { RememberMe = rememberMe }, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(saved);
        Assert.Equal("Firefox en Linux", saved.Device);
        Assert.Equal(user.UserId, saved.UserId);
        Assert.Equal(TimeSpan.FromHours(expectedHours), saved.ExpiresAt - saved.CreatedAt);
        Assert.Equal(saved.SessionId!.Value, result.Value.SessionId);
        Assert.Equal(user.UserId!.Value, result.Value.UserId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Malo: nada revela si el correo existe, y ningún rechazo registra una sesión.

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        UserExists(passwordMatches: false);

        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0015", result.FirstError.Code);
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        VerifyNoSession();
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsSameErrorAndStillHashes()
    {
        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0015", result.FirstError.Code);
        _passwordHasher.Verify(h => h.Hash(ValidRequest.Password), Times.Once);
        VerifyNoSession();
    }

    [Fact]
    public async Task LoginAsync_WithDeletedUser_ReturnsInvalidCredentials()
    {
        // Arrange
        var user = CreateUser();
        user.Delete();
        UserExists(user);

        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0015", result.FirstError.Code);
        VerifyNoSession();
    }

    [Fact]
    public async Task LoginAsync_WithMalformedEmail_ReturnsInvalidCredentials()
    {
        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest with { Email = "sin-arroba" }, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0015", result.FirstError.Code);
        VerifyNoSession();
    }

    [Fact]
    public async Task LoginAsync_WithSuspendedUserAndCorrectPassword_ReturnsAccountUnavailable()
    {
        // Arrange
        var user = CreateUser();
        user.Suspend();
        UserExists(user);

        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0016", result.FirstError.Code);
        VerifyNoSession();
    }

    [Fact]
    public async Task LoginAsync_WithSuspendedUserAndWrongPassword_DoesNotRevealStatus()
    {
        // Arrange
        var user = CreateUser();
        user.Suspend();
        UserExists(user, passwordMatches: false);

        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0015", result.FirstError.Code);
    }

    [Fact]
    public async Task LoginAsync_WithTemporarilyLockedUser_ReturnsAccountUnavailable()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateAccountLockedUntil(DateTime.UtcNow.AddHours(1));
        UserExists(user);

        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0016", result.FirstError.Code);
        VerifyNoSession();
    }

    [Theory]
    [InlineData("", "Correct-password1", "SECURITY-0003")]
    [InlineData("brian@caudal.com", "", "SECURITY-0006")]
    public async Task LoginAsync_WithMissingData_ReturnsValidationError(string email, string password, string expectedCode)
    {
        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest with { Email = email, Password = password }, CancellationToken.None);

        // Assert
        Assert.Equal(expectedCode, result.FirstError.Code);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task LoginAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
        _users
            .Setup(u => u.GetByEmailAsync(It.IsAny<Email?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        // Act
        var result = await CreateUseCase().LoginAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0011", result.FirstError.Code);
    }
}
