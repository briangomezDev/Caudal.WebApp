using Microsoft.AspNetCore.Antiforgery;

namespace Caudal.BlazorApp.Modules.Account.Session;

public static class SessionEndpoints
{
    private const string LoginPath = "/account/login";

    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/logout", async (HttpContext httpContext, IAntiforgery antiforgery, SessionFacade sessions) =>
        {
            // Sin el token, otro sitio podría cerrar la sesión del usuario con un formulario oculto.
            // No es una página de error: también pasa en una pestaña vieja, cuya sesión ya se cerró en otra.
            if (!await antiforgery.IsRequestValidAsync(httpContext))
                return Results.LocalRedirect(httpContext.User.Identity?.IsAuthenticated == true ? "/" : LoginPath);

            await sessions.LogoutAsync(httpContext);

            return Results.LocalRedirect(LoginPath);
        });

        return endpoints;
    }
}
