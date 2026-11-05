using ErrorOr;

namespace Caudal.Application.Services.AppUser.Logout;

public interface IUserLogoutUseCase
{
    Task<ErrorOr<bool>> LogoutAsync(Guid sessionId, CancellationToken cancellationToken);
}
