using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;

namespace Caudal.BlazorApp.Modules.Account.Session;

/// <summary>
/// La cookie solo dice qué sesión reclama el usuario; la fila de app_user_session decide si sigue vigente.
/// </summary>
public sealed class SessionCookieEvents(SessionFacade sessions) : CookieAuthenticationEvents
{
    public const string ExpiredReason = "expired";

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        // La autenticación corre en todas las peticiones: sin esto, cada archivo estático consultaría la base.
        if (!RequiresSession(context.HttpContext.GetEndpoint()))
            return;

        var check = await sessions.CheckAsync(SessionClaims.SessionIdOf(context.Principal), context.HttpContext.RequestAborted);

        if (!check.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }

        if (check.RenewedUntil is { } expiresAt)
        {
            context.Properties.ExpiresUtc = new DateTimeOffset(expiresAt);
            context.ShouldRenew = true;
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        // Traía cookie y aun así hay que entrar de nuevo: su sesión terminó y merece una explicación.
        // Sin cookie es una primera visita o un cierre de sesión normal, y no hay nada que explicar.
        if (context.Request.Cookies.ContainsKey(context.Options.Cookie.Name!))
            context.RedirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "reason", ExpiredReason);

        return base.RedirectToLogin(context);
    }

    private static bool RequiresSession(Endpoint? endpoint) =>
        endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null
        && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;
}
