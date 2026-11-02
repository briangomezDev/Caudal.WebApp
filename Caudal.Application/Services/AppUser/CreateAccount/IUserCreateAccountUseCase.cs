using Caudal.Domain.Security.Entity;
using ErrorOr;

namespace Caudal.Application.Services.AppUser.CreateAccount;

public interface IUserCreateAccountUseCase
{
    Task<ErrorOr<bool>> CreateAccountAsync(UserRequest user, CancellationToken cancellationToken);
}
