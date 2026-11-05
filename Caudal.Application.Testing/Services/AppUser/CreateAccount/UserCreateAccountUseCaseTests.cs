using Caudal.Application.Services.AppUser.Confirmation;
using Caudal.Application.Services.AppUser.CreateAccount;
using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Enums;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Caudal.Application.Testing.Services.AppUser.CreateAccount;

public class UserCreateAccountUseCaseTests
{
    private static readonly UserRequest ValidRequest = new("Brian Gomez", "Brian@Caudal.com", "Correct-password1");

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAccountConfirmationTokens> _tokens = new();
    private readonly Mock<IAccountNotifier> _notifier = new();

    public UserCreateAccountUseCaseTests()
    {
        _passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns(("hashed-password", new SaltCode("abc123")));
        _tokens
            .Setup(t => t.Create(It.IsAny<UserId>(), It.IsAny<Email>()))
            .Returns("signed+token");
    }

    private static User CreateExistingUser(DateTime? createdAt = null, bool confirmed = false)
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            "Otra Persona",
            Email.Create("brian@caudal.com"),
            "other-hash",
            new SaltCode("xyz789"),
            createdAt ?? DateTime.UtcNow);

        if (confirmed)
            user.UpdateEmailVerifiedAt(DateTime.UtcNow);

        return user;
    }

    // Confirmación: el registro envía el enlace, sin que un fallo de envío pierda la cuenta.

    [Fact]
    public async Task CreateAccountAsync_WithValidRequest_SendsConfirmationLink()
    {
        // Act
        await CreateUseCase().CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        _notifier.Verify(n => n.SendConfirmationAsync(
            It.Is<AccountRecipient>(r => r.FullName == "Brian Gomez" && r.Email == "brian@caudal.com"),
            "https://caudal.test/account/confirm?token=signed%2Btoken",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenSendingFails_StillCreatesTheAccount()
    {
        // Arrange
        _notifier
            .Setup(n => n.SendConfirmationAsync(It.IsAny<AccountRecipient>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        // Act
        var result = await CreateUseCase().CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenUnconfirmedAccountExpired_ReplacesIt()
    {
        // Arrange
        var expired = CreateExistingUser(DateTime.UtcNow.AddHours(-25));
        _repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        // Act
        var result = await CreateUseCase().CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _repository.Verify(r => r.RemoveUser(expired), Times.Once);
        _repository.Verify(r => r.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 100)]
    public async Task CreateAccountAsync_WhenAccountStillValid_ReturnsConflictAndKeepsIt(bool confirmed, int hoursOld)
    {
        // Arrange
        var existing = CreateExistingUser(DateTime.UtcNow.AddHours(-hoursOld), confirmed);
        _repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await CreateUseCase().CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.Equal("SECURITY-0012", result.FirstError.Code);
        _repository.Verify(r => r.RemoveUser(It.IsAny<User>()), Times.Never);
        VerifyNothingWasSaved();
    }

    private UserCreateAccountUseCase CreateUseCase(IValidator<UserRequest>? validator = null)
    {
        return new UserCreateAccountUseCase(
            NullLogger<UserCreateAccountUseCase>.Instance,
            _unitOfWork.Object,
            _repository.Object,
            validator ?? new UserRequestValidator(),
            _passwordHasher.Object,
            new AccountConfirmationSender(
                NullLogger<AccountConfirmationSender>.Instance,
                _tokens.Object,
                _notifier.Object,
                Options.Create(new AccountLinkOptions { BaseUrl = "https://caudal.test/" })));
    }

    private void VerifyNothingWasSaved()
    {
        _repository.Verify(r => r.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Bueno: el flujo esperado se completa correctamente.

    [Fact]
    public async Task CreateAccountAsync_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task CreateAccountAsync_WithValidRequest_AddsActiveUserWithHashedPassword()
    {
        // Arrange
        User? addedUser = null;
        _repository
            .Setup(r => r.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => addedUser = user)
            .Returns(Task.CompletedTask);
        var useCase = CreateUseCase();

        // Act
        await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.NotNull(addedUser);
        Assert.NotEqual(Guid.Empty, addedUser.UserId!.Value);
        Assert.Equal("Brian Gomez", addedUser.FullName);
        Assert.Equal("brian@caudal.com", addedUser.Email!.Value);
        Assert.Equal("hashed-password", addedUser.Password);
        Assert.Equal(new SaltCode("abc123"), addedUser.SaltCode);
        Assert.Equal(UserStatus.Active, addedUser.Status);
        Assert.Equal(DateTimeKind.Utc, addedUser.AccountCreatedAt!.Value.Kind);
        _passwordHasher.Verify(h => h.Hash("Correct-password1"), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_WithCancellationToken_PassesTokenToDependencies()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var useCase = CreateUseCase();

        // Act
        await useCase.CreateAccountAsync(ValidRequest, cancellation.Token);

        // Assert
        _repository.Verify(r => r.GetByEmailAsync(It.IsAny<Email>(), cancellation.Token), Times.Once);
        _repository.Verify(r => r.AddUserAsync(It.IsAny<User>(), cancellation.Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    // Malo: los datos inválidos o las reglas de negocio rechazan la solicitud.

    [Fact]
    public async Task CreateAccountAsync_WithInvalidRequest_ReturnsAllValidationErrorsAndSavesNothing()
    {
        // Arrange
        var request = new UserRequest("", "invalid-email", "short");
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.All(result.Errors, error => Assert.Equal(ErrorType.Validation, error.Type));
        Assert.Equal(
            ["SECURITY-0001", "SECURITY-0005", "SECURITY-0007"],
            result.Errors.Select(error => error.Code));
        _repository.Verify(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenEmailAlreadyExists_ReturnsConflictAndSavesNothing()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByEmailAsync(Email.Create("brian@caudal.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateExistingUser());
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        Assert.Equal("SECURITY-0012", result.FirstError.Code);
        _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenEmailIsTakenWhileSaving_ReturnsConflict()
    {
        // Arrange
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintException("uq_app_user_email", "A unique constraint was violated."));
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        Assert.Equal("SECURITY-0012", result.FirstError.Code);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenOtherUniqueConstraintFails_DoesNotReturnEmailConflict()
    {
        // Arrange
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintException("uq_other_constraint", "A unique constraint was violated."));
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.NotEqual(ErrorType.Conflict, result.FirstError.Type);
        Assert.Equal("SECURITY-0010", result.FirstError.Code);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenDomainRejectsData_ReturnsValidationErrorAndSavesNothing()
    {
        // Arrange
        var validator = new Mock<IValidator<UserRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<UserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        var request = new UserRequest("Brian Gomez", "email-without-at", "correct-password");
        var useCase = CreateUseCase(validator.Object);

        // Act
        var result = await useCase.CreateAccountAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("SECURITY-0010", result.FirstError.Code);
        VerifyNothingWasSaved();
    }

    [Theory]
    [InlineData("logger")]
    [InlineData("unitOfWork")]
    [InlineData("repository")]
    [InlineData("validator")]
    [InlineData("passwordHasher")]
    [InlineData("confirmationSender")]
    public void Constructor_WithNullDependency_ThrowsException(string parameter)
    {
        // Arrange
        var logger = parameter == "logger" ? null! : NullLogger<UserCreateAccountUseCase>.Instance;
        var unitOfWork = parameter == "unitOfWork" ? null! : _unitOfWork.Object;
        var repository = parameter == "repository" ? null! : _repository.Object;
        IValidator<UserRequest> validator = parameter == "validator" ? null! : new UserRequestValidator();
        var passwordHasher = parameter == "passwordHasher" ? null! : _passwordHasher.Object;
        var confirmationSender = parameter == "confirmationSender" ? null! : new AccountConfirmationSender(
            NullLogger<AccountConfirmationSender>.Instance,
            _tokens.Object,
            _notifier.Object,
            Options.Create(new AccountLinkOptions { BaseUrl = "https://caudal.test/" }));

        // Act
        var act = () => new UserCreateAccountUseCase(logger, unitOfWork, repository, validator, passwordHasher, confirmationSender);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal(parameter, exception.ParamName);
    }

    // Fallo: una dependencia falla de forma inesperada.

    [Fact]
    public async Task CreateAccountAsync_WhenValidatorFails_ReturnsFailureError()
    {
        // Arrange
        var validator = new Mock<IValidator<UserRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<UserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Validator failure."));
        var useCase = CreateUseCase(validator.Object);

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("SECURITY-0011", result.FirstError.Code);
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenExistsCheckFails_ReturnsFailureError()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timeout."));
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("SECURITY-0011", result.FirstError.Code);
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenPasswordHasherFails_ReturnsFailureError()
    {
        // Arrange
        _passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Hasher failure."));
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("SECURITY-0011", result.FirstError.Code);
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenAddUserFails_ReturnsFailureErrorAndDoesNotSave()
    {
        // Arrange
        _repository
            .Setup(r => r.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timeout."));
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("SECURITY-0011", result.FirstError.Code);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenSaveChangesFails_ReturnsFailureError()
    {
        // Arrange
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timeout."));
        var useCase = CreateUseCase();

        // Act
        var result = await useCase.CreateAccountAsync(ValidRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("SECURITY-0011", result.FirstError.Code);
    }
}
