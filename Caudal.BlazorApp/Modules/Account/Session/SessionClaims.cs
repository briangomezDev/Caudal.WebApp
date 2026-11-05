using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Caudal.BlazorApp.Modules.Account.Session;

// Lo único que viaja en la cookie (cifrada): quién es el usuario y qué fila de app_user_session lo respalda.
public static class SessionClaims
{
    public static ClaimsPrincipal CreatePrincipal(Guid userId, Guid sessionId, string fullName)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Sid, sessionId.ToString()),
                new Claim(ClaimTypes.Name, fullName)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    public static Guid SessionIdOf(ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirstValue(ClaimTypes.Sid), out var sessionId) ? sessionId : Guid.Empty;
}
