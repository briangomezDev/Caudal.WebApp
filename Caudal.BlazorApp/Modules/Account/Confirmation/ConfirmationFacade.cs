using Caudal.Application.Services.AppUser.Confirmation;

namespace Caudal.BlazorApp.Modules.Account.Confirmation;

public enum ConfirmationOutcome
{
    Confirmed,
    InvalidLink,
    Failed
}

/// <summary>
/// Puerta de la feature de confirmación hacia Application.
/// </summary>
public sealed class ConfirmationFacade(IServiceScopeFactory scopes)
{
    public async Task<ConfirmationOutcome> ConfirmAsync(string? token, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUserConfirmationUseCase>();

        var result = await useCase.ConfirmAsync(token ?? string.Empty, cancellationToken);

        if (!result.IsError)
            return ConfirmationOutcome.Confirmed;

        return result.FirstError.Code == "SECURITY-0019" ? ConfirmationOutcome.InvalidLink : ConfirmationOutcome.Failed;
    }

    public async Task ResendAsync(string? email, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUserConfirmationUseCase>();

        await useCase.ResendAsync(email?.Trim() ?? string.Empty, cancellationToken);
    }
}
