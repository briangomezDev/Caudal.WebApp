using ErrorOr;

namespace Caudal.Application.Services.AppUser.ValidateSession;

public interface IUserSessionValidationUseCase
{
    Task<ErrorOr<SessionValidationResponse>> ValidateAsync(Guid sessionId, CancellationToken cancellationToken);
}
