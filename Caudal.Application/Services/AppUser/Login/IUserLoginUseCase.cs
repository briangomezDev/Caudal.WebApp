using ErrorOr;

namespace Caudal.Application.Services.AppUser.Login;

public interface IUserLoginUseCase
{
    Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
