using Caudal.Application.Services.AppUser.Confirmation;
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
    private readonly AccountConfirmationSender _confirmationSender;

    public UserCreateAccountUseCase(ILogger<UserCreateAccountUseCase> logger,
                                IUnitOfWork unitOfWork,
                                IUserRepository repository,
                                IValidator<UserRequest> validator,
                                IPasswordHasher passwordHasher,
                                AccountConfirmationSender confirmationSender)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _confirmationSender = confirmationSender ?? throw new ArgumentNullException(nameof(confirmationSender));
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
            var now = DateTime.UtcNow;

            var existing = await _repository.GetByEmailAsync(email, cancellationToken);

            if (existing is not null)
            {
                if (!existing.HasConfirmationExpired(now))
                    return Error.Conflict("SECURITY-0012", "User already exists with this email");

                // Nadie confirmó a tiempo: libera el correo sin necesidad de un job que limpie estas cuentas.
                _repository.RemoveUser(existing);
            }

            UserId id = new UserId(Guid.NewGuid());

            (string PasswordHash, SaltCode Salt) = _passwordHasher.Hash(request.Password);

            User user = new User(
                id,
                request.FullName,
                email,
                PasswordHash,
                Salt,
                now
            );

            await _repository.AddUserAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _confirmationSender.SendAsync(user, cancellationToken);

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
