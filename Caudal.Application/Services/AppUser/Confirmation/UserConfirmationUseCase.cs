using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Domain.Shared.ValueObjects;
using Caudal.Infraestructure;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Caudal.Application.Services.AppUser.Confirmation;

internal sealed class UserConfirmationUseCase : IUserConfirmationUseCase
{
    private readonly ILogger<UserConfirmationUseCase> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;
    private readonly IAccountConfirmationTokens _tokens;
    private readonly AccountConfirmationSender _sender;

    public UserConfirmationUseCase(ILogger<UserConfirmationUseCase> logger,
                                   IUnitOfWork unitOfWork,
                                   IUserRepository users,
                                   IAccountConfirmationTokens tokens,
                                   AccountConfirmationSender sender)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<ErrorOr<ConfirmationResult>> ConfirmAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || !_tokens.TryRead(token, out var claim))
                return InvalidLink();

            var user = await _users.GetByIdAsync(claim!.UserId, cancellationToken);

            // El correo del enlace debe seguir siendo el de la cuenta.
            if (user is null || user.Email != claim.Email)
                return InvalidLink();

            if (user.IsEmailConfirmed)
                return ConfirmationResult.AlreadyConfirmed;

            // Solo guarda la fecha y no toca el estado: un enlace viejo no puede reactivar una cuenta sancionada.
            user.UpdateEmailVerifiedAt(DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ConfirmationResult.Confirmed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while confirming an account");
            return Error.Failure("SECURITY-0011", "An unexpected error occurred");
        }
    }

    public async Task ResendAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _users.GetByEmailAsync(Email.Create(email), cancellationToken);

            if (user is { IsEmailConfirmed: false, IsEnabled: true })
                await _sender.SendAsync(user, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DomainException)
        {
            // Un correo mal escrito no tiene cuenta: misma respuesta que cualquier otro.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while resending an account confirmation");
        }
    }

    private static Error InvalidLink() =>
        Error.Validation("SECURITY-0019", "The confirmation link is invalid or expired");
}
