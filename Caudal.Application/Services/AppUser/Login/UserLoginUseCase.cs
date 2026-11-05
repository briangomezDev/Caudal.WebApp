using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Enums;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.Session.Adapter;
using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Caudal.Application.Services.AppUser.Login;

internal sealed class UserLoginUseCase : IUserLoginUseCase
{
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(8);
    private static readonly TimeSpan RememberedSessionDuration = TimeSpan.FromDays(30);

    private readonly ILogger<UserLoginUseCase> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;
    private readonly IUserSessionRepository _sessions;
    private readonly IValidator<LoginRequest> _validator;
    private readonly IPasswordHasher _passwordHasher;

    public UserLoginUseCase(ILogger<UserLoginUseCase> logger,
                            IUnitOfWork unitOfWork,
                            IUserRepository users,
                            IUserSessionRepository sessions,
                            IValidator<LoginRequest> validator,
                            IPasswordHasher passwordHasher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
                return result.Errors.Select(x =>
                Error.Validation(x.ErrorCode, x.ErrorMessage)).ToList();

            if (!TryCreateEmail(request.Email, out var email))
                return InvalidCredentials();

            var user = await _users.GetByEmailAsync(email, cancellationToken);

            if (user is null || user.Status == UserStatus.Deleted)
            {
                // Mismo costo que una contraseña incorrecta: el tiempo de respuesta no revela si el correo existe.
                _passwordHasher.Hash(request.Password);
                return InvalidCredentials();
            }

            var passwordMatches = _passwordHasher.Verify(request.Password, user.Password!, user.SaltCode!);
            var now = DateTime.UtcNow;

            // Misma respuesta con cualquier contraseña: si fuera distinta, se podría seguir adivinando durante el bloqueo.
            if (user.IsLockedOut(now))
                return InvalidCredentials();

            if (!passwordMatches)
            {
                await _users.RegisterFailedLoginAsync(user.UserId!, now, cancellationToken);
                return InvalidCredentials();
            }

            // Va después de la contraseña: solo quien la conoce sabe que la cuenta no está disponible.
            if (!user.IsEnabled)
                return Error.Forbidden("SECURITY-0016", "The account is not available");

            user.ResetFailedLogins();

            var duration = request.RememberMe ? RememberedSessionDuration : SessionDuration;
            var session = new UserSession(new SessionId(Guid.NewGuid()), user.UserId!, request.Device, now, now + duration);

            await _sessions.RemoveExpiredByUserAsync(user.UserId!, now, cancellationToken);
            await _sessions.AddAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new LoginResponse(user.UserId!.Value, session.SessionId!.Value, user.FullName ?? string.Empty, session.ExpiresAt);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while logging in");
            return Error.Failure("SECURITY-0011", "An unexpected error occurred");
        }
    }

    private static bool TryCreateEmail(string value, out Email? email)
    {
        try
        {
            email = Email.Create(value);
            return true;
        }
        catch (DomainException)
        {
            email = null;
            return false;
        }
    }

    private static Error InvalidCredentials() =>
        Error.Unauthorized("SECURITY-0015", "The email or password is incorrect");
}
