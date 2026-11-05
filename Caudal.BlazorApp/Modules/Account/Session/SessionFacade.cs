using Caudal.Application.Services.AppUser.Logout;
using Caudal.Application.Services.AppUser.ValidateSession;
using ErrorOr;
using Microsoft.AspNetCore.Authentication;

namespace Caudal.BlazorApp.Modules.Account.Session;

public sealed record SessionCheck(bool IsActive, DateTime? RenewedUntil);

/// <summary>
/// Puerta de la feature de sesión hacia Application: comprueba y termina sesiones.
/// </summary>
public sealed class SessionFacade(IServiceScopeFactory scopes)
{
    public async Task<SessionCheck> CheckAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUserSessionValidationUseCase>();

        var result = await useCase.ValidateAsync(sessionId, cancellationToken);

        if (!result.IsError)
            return new SessionCheck(true, result.Value.Renewed ? result.Value.ExpiresAt : null);

        // Que la base de datos falle no significa que la sesión sea inválida: no se expulsa al usuario.
        if (result.FirstError.Type != ErrorType.Unauthorized)
            throw new InvalidOperationException($"The session could not be checked: {result.FirstError.Code}");

        return new SessionCheck(false, null);
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        await using var scope = scopes.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUserLogoutUseCase>();

        // Aunque falle el registro en la base de datos, el usuario que pidió salir debe salir.
        await useCase.LogoutAsync(SessionClaims.SessionIdOf(httpContext.User), httpContext.RequestAborted);
        await httpContext.SignOutAsync();
    }
}
