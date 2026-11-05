using Microsoft.AspNetCore.Antiforgery;

namespace Caudal.BlazorApp.Modules.Account.Session;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/logout", async (HttpContext httpContext, IAntiforgery antiforgery, SessionFacade sessions) =>
        {
            // Sin el token, otro sitio podría cerrar la sesión del usuario con un formulario oculto.
            if (!await antiforgery.IsRequestValidAsync(httpContext))
                return Results.BadRequest();

            await sessions.LogoutAsync(httpContext);

            return Results.LocalRedirect("/account/login");
        });

        return endpoints;
    }
}
