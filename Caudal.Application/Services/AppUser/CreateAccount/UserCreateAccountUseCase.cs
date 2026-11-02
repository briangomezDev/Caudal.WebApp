using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Caudal.Application.Services.AppUser.CreateAccount;

internal sealed class UserCreateAccountUseCase : IUserCreateAccountUseCase
{
    private readonly ILogger<UserCreateAccountUseCase> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _repository;
    private readonly IValidator<UserRequest> _validator;
    private readonly IPasswordHasher _passwordHasher;

    public UserCreateAccountUseCase(ILogger<UserCreateAccountUseCase> logger,
                                IUnitOfWork unitOfWork,
                                IUserRepository repository,
                                IValidator<UserRequest> validator,
                                IPasswordHasher passwordHasher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }
    public async Task<ErrorOr<bool>> CreateAccountAsync(UserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
                return result.Errors.Select(x =>
                Error.Validation(x.ErrorCode, x.ErrorMessage)).ToList();

            Email email = Email.Create(request.Email);

            var userAlredyExists = await _repository.ExistsByEmailAsync(email, cancellationToken);

            if (userAlredyExists)
                return Error.Conflict("SECURITY-0012", "User already exists with this email");

            UserId id = new UserId(Guid.NewGuid());

            (string PasswordHash, SaltCode Salt) = _passwordHasher.Hash(request.Password);

            User user = new User(
                id,
                request.FullName,
                email,
                PasswordHash,
                Salt,
                DateTime.UtcNow
            );

            await _repository.AddUserAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (UniqueConstraintException ex) when (ex.ConstraintName == "uq_app_user_email")
        {
            return Error.Conflict("SECURITY-0012", "User already exists with this email");
        }
        catch (DomainException ex)
        {
            return Error.Validation("SECURITY-0010", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating a user account");
            return Error.Failure("SECURITY-0011", "An unexpected error occurred");
        }
    }
}
