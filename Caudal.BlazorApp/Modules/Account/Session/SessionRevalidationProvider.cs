using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Caudal.BlazorApp.Modules.Account.Session;

/// <summary>
/// Un circuito interactivo no vuelve a pasar por la cookie mientras está abierto: esto comprueba
/// periódicamente que su sesión siga vigente y, si terminó, recarga la página para que la cookie decida.
/// </summary>
public sealed class SessionRevalidationProvider(
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    SessionFacade sessions,
    NavigationManager navigation) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval { get; } =
        configuration.GetValue("Session:RevalidationInterval", TimeSpan.FromMinutes(5));

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        SessionCheck check;

        try
        {
            check = await sessions.CheckAsync(SessionClaims.SessionIdOf(authenticationState.User), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Que la base de datos falle no significa que la sesión sea inválida: se reintenta en el siguiente ciclo.
            return true;
        }

        if (!check.IsActive)
            navigation.Refresh(forceReload: true);

        return check.IsActive;
    }
}
