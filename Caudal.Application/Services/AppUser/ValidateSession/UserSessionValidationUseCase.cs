using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Session.Adapter;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Caudal.Application.Services.AppUser.ValidateSession;

internal sealed class UserSessionValidationUseCase : IUserSessionValidationUseCase
{
    // Evita escribir en la base de datos en cada página.
    private static readonly TimeSpan RenewalInterval = TimeSpan.FromMinutes(1);

    private readonly ILogger<UserSessionValidationUseCase> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSessionRepository _sessions;
    private readonly IUserRepository _users;

    public UserSessionValidationUseCase(ILogger<UserSessionValidationUseCase> logger,
                                        IUnitOfWork unitOfWork,
                                        IUserSessionRepository sessions,
                                        IUserRepository users)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<ErrorOr<SessionValidationResponse>> ValidateAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            if (sessionId == Guid.Empty)
                return InvalidSession();

            var session = await _sessions.GetByIdAsync(new SessionId(sessionId), cancellationToken);
            var now = DateTime.UtcNow;

            if (session is null || !session.IsActive(now))
                return InvalidSession();

            // Suspender o eliminar a un usuario también termina las sesiones que tenga abiertas.
            var user = await _users.GetByIdAsync(session.UserId!, cancellationToken);
            if (user is null || !user.IsEnabled)
            {
                session.Expire(now);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return InvalidSession();
            }

            if (now - (session.LastActivityAt ?? session.CreatedAt) < RenewalInterval)
                return new SessionValidationResponse(session.ExpiresAt, Renewed: false);

            session.Renew(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new SessionValidationResponse(session.ExpiresAt, Renewed: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while validating a session");
            return Error.Failure("SECURITY-0011", "An unexpected error occurred");
        }
    }

    private static Error InvalidSession() =>
        Error.Unauthorized("SECURITY-0017", "The session is not valid");
}
