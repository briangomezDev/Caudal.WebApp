using Caudal.Application.Services.AppUser.Confirmation;
using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Caudal.Application.Testing.Services.AppUser.Confirmation;

public class UserConfirmationUseCaseTests
{
    private const string Token = "signed-token";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAccountConfirmationTokens> _tokens = new();
    private readonly Mock<IAccountNotifier> _notifier = new();

    private UserConfirmationUseCase CreateUseCase() => new(
        NullLogger<UserConfirmationUseCase>.Instance,
        _unitOfWork.Object,
        _users.Object,
        _tokens.Object,
        new AccountConfirmationSender(
            NullLogger<AccountConfirmationSender>.Instance,
            _tokens.Object,
            _notifier.Object,
            Options.Create(new AccountLinkOptions { BaseUrl = "https://caudal.test" })));

    private static User CreateUser(string email = "brian@caudal.com") => new(
        new UserId(Guid.NewGuid()),
        "Brian Gomez",
        Email.Create(email),
        "hashed-password",
        new SaltCode("abc123"),
        DateTime.UtcNow);

    private User LinkPointsTo(User user, string? claimedEmail = null)
    {
        var claim = new AccountConfirmationClaim(user.UserId!, Email.Create(claimedEmail ?? user.Email!.Value!));
        _tokens.Setup(t => t.TryRead(Token, out claim)).Returns(true);
        _users.Setup(u => u.GetByIdAsync(user.UserId!, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    private void VerifyNotSent() =>
        _notifier.Verify(n => n.SendConfirmationAsync(It.IsAny<AccountRecipient>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

    // Bueno: un enlace válido confirma, y repetirlo no es un error.

    [Fact]
    public async Task ConfirmAsync_WithValidLink_SetsVerificationDate()
    {
        // Arrange
        var user = LinkPointsTo(CreateUser());

        // Act
        var result = await CreateUseCase().ConfirmAsync(Token, CancellationToken.None);

        // Assert
        Assert.Equal(ConfirmationResult.Confirmed, result.Value);
        Assert.True(user.IsEmailConfirmed);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_WhenAlreadyConfirmed_KeepsOriginalDateAndDoesNotWrite()
    {
        // Arrange
        var user = CreateUser();
        var confirmedAt = DateTime.UtcNow.AddDays(-1);
        user.UpdateEmailVerifiedAt(confirmedAt);
        LinkPointsTo(user);

        // Act
        var result = await CreateUseCase().ConfirmAsync(Token, CancellationToken.None);

        // Assert
        Assert.Equal(ConfirmationResult.AlreadyConfirmed, result.Value);
        Assert.Equal(confirmedAt, user.EmailVerifiedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendAsync_WithUnconfirmedAccount_SendsLink()
    {
        // Arrange
        var user = CreateUser();
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokens.Setup(t => t.Create(It.IsAny<UserId>(), It.IsAny<Email>())).Returns(Token);

        // Act
        await CreateUseCase().ResendAsync("brian@caudal.com", CancellationToken.None);

        // Assert
        _notifier.Verify(n => n.SendConfirmationAsync(
            It.IsAny<AccountRecipient>(),
            "https://caudal.test/account/confirm?token=signed-token",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Malo: los enlaces inválidos se rechazan, y una sanción no se puede saltar.

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("alterado")]
    public async Task ConfirmAsync_WithInvalidToken_ReturnsInvalidLink(string token)
    {
        // Act
        var result = await CreateUseCase().ConfirmAsync(token, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0019", result.FirstError.Code);
    }

    [Fact]
    public async Task ConfirmAsync_WithDeletedAccount_ReturnsInvalidLink()
    {
        // Arrange
        var claim = new AccountConfirmationClaim(new UserId(Guid.NewGuid()), Email.Create("brian@caudal.com"));
        _tokens.Setup(t => t.TryRead(Token, out claim)).Returns(true);

        // Act
        var result = await CreateUseCase().ConfirmAsync(Token, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0019", result.FirstError.Code);
    }

    [Fact]
    public async Task ConfirmAsync_WhenAccountEmailChanged_ReturnsInvalidLink()
    {
        // Arrange
        var user = LinkPointsTo(CreateUser(), claimedEmail: "anterior@caudal.com");

        // Act
        var result = await CreateUseCase().ConfirmAsync(Token, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0019", result.FirstError.Code);
        Assert.False(user.IsEmailConfirmed);
    }

    [Fact]
    public async Task ConfirmAsync_WithLockedAccount_ConfirmsWithoutReactivating()
    {
        // Arrange
        var user = CreateUser();
        user.Lock();
        LinkPointsTo(user);

        // Act
        await CreateUseCase().ConfirmAsync(Token, CancellationToken.None);

        // Assert
        Assert.True(user.IsEmailConfirmed);
        Assert.False(user.IsEnabled);
    }

    [Fact]
    public async Task ConfirmAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
        var claim = new AccountConfirmationClaim(new UserId(Guid.NewGuid()), Email.Create("brian@caudal.com"));
        _tokens.Setup(t => t.TryRead(Token, out claim)).Returns(true);
        _users.Setup(u => u.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("database down"));

        // Act
        var result = await CreateUseCase().ConfirmAsync(Token, CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
    }

    [Fact]
    public async Task ResendAsync_WithUnknownEmail_DoesNothingAndDoesNotFail()
    {
        // Act
        await CreateUseCase().ResendAsync("nadie@caudal.com", CancellationToken.None);

        // Assert
        VerifyNotSent();
    }

    [Fact]
    public async Task ResendAsync_WithMalformedEmail_DoesNothingAndDoesNotFail()
    {
        // Act
        await CreateUseCase().ResendAsync("sin-arroba", CancellationToken.None);

        // Assert
        VerifyNotSent();
    }

    [Fact]
    public async Task ResendAsync_WithConfirmedAccount_DoesNotSend()
    {
        // Arrange
        var user = CreateUser();
        user.UpdateEmailVerifiedAt(DateTime.UtcNow);
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        await CreateUseCase().ResendAsync("brian@caudal.com", CancellationToken.None);

        // Assert
        VerifyNotSent();
    }

    [Fact]
    public async Task ResendAsync_WithSuspendedAccount_DoesNotSend()
    {
        // Arrange
        var user = CreateUser();
        user.Suspend();
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        await CreateUseCase().ResendAsync("brian@caudal.com", CancellationToken.None);

        // Assert
        VerifyNotSent();
    }
}
