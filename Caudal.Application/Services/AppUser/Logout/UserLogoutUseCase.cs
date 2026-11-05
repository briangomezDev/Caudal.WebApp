using Caudal.Domain.Security.Session.Adapter;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Caudal.Application.Services.AppUser.Logout;

internal sealed class UserLogoutUseCase : IUserLogoutUseCase
{
    private readonly ILogger<UserLogoutUseCase> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSessionRepository _sessions;

    public UserLogoutUseCase(ILogger<UserLogoutUseCase> logger,
                             IUnitOfWork unitOfWork,
                             IUserSessionRepository sessions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    public async Task<ErrorOr<bool>> LogoutAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            if (sessionId == Guid.Empty)
                return true;

            var session = await _sessions.GetByIdAsync(new SessionId(sessionId), cancellationToken);
            var now = DateTime.UtcNow;

            // Cerrar una sesión que ya terminó no es un error, y conserva su fecha real de expiración.
            if (session is null || !session.IsActive(now))
                return true;

            session.Expire(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while logging out");
            return Error.Failure("SECURITY-0011", "An unexpected error occurred");
        }
    }
}
