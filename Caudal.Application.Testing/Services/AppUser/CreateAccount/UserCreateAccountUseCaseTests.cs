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
using Moq;
using Xunit;

namespace Caudal.Application.Testing.Services.AppUser.CreateAccount;

public class UserCreateAccountUseCaseTests
{
    private static readonly UserRequest ValidRequest = new("Brian Gomez", "Brian@Caudal.com", "correct-password");

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    public UserCreateAccountUseCaseTests()
    {
        _passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns(("hashed-password", new SaltCode("abc123")));
    }

    private UserCreateAccountUseCase CreateUseCase(IValidator<UserRequest>? validator = null)
    {
        return new UserCreateAccountUseCase(
            NullLogger<UserCreateAccountUseCase>.Instance,
            _unitOfWork.Object,
            _repository.Object,
            validator ?? new UserRequestValidator(),
            _passwordHasher.Object);
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
        _passwordHasher.Verify(h => h.Hash("correct-password"), Times.Once);
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
        _repository.Verify(r => r.ExistsByEmailAsync(It.IsAny<Email>(), cancellation.Token), Times.Once);
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
        _repository.Verify(r => r.ExistsByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
        VerifyNothingWasSaved();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenEmailAlreadyExists_ReturnsConflictAndSavesNothing()
    {
        // Arrange
        _repository
            .Setup(r => r.ExistsByEmailAsync(Email.Create("brian@caudal.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
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
    public void Constructor_WithNullDependency_ThrowsException(string parameter)
    {
        // Arrange
        var logger = parameter == "logger" ? null! : NullLogger<UserCreateAccountUseCase>.Instance;
        var unitOfWork = parameter == "unitOfWork" ? null! : _unitOfWork.Object;
        var repository = parameter == "repository" ? null! : _repository.Object;
        IValidator<UserRequest> validator = parameter == "validator" ? null! : new UserRequestValidator();
        var passwordHasher = parameter == "passwordHasher" ? null! : _passwordHasher.Object;

        // Act
        var act = () => new UserCreateAccountUseCase(logger, unitOfWork, repository, validator, passwordHasher);

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
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
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
