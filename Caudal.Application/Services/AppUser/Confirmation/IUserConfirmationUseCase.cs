using ErrorOr;

namespace Caudal.Application.Services.AppUser.Confirmation;

public enum ConfirmationResult
{
    Confirmed,
    AlreadyConfirmed
}

public interface IUserConfirmationUseCase
{
    Task<ErrorOr<ConfirmationResult>> ConfirmAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Devuelve lo mismo exista o no la cuenta, para no revelar qué correos están registrados.
    /// </summary>
    Task ResendAsync(string email, CancellationToken cancellationToken);
}
