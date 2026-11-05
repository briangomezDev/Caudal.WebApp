using Caudal.Application.Services.AppUser.Login;
using Caudal.BlazorApp.Modules.Account.Session;
using ErrorOr;
using Microsoft.AspNetCore.Authentication;

namespace Caudal.BlazorApp.Modules.Account.Login;

/// <summary>
/// Única puerta de la feature hacia Application: registra la sesión y la deja en la cookie.
/// </summary>
public sealed class LoginFacade(IServiceScopeFactory scopes)
{
    private const string DefaultTarget = "/";

    private static readonly Dictionary<string, string> MessagesByCode = new()
    {
        ["SECURITY-0003"] = "Escribe tu correo y tu contraseña",
        ["SECURITY-0006"] = "Escribe tu correo y tu contraseña",
        ["SECURITY-0015"] = "Correo o contraseña incorrectos",
        ["SECURITY-0016"] = "Tu cuenta no está disponible. Contacta a soporte.",
    };

    /// <returns>El mensaje de error, o null si la sesión quedó iniciada.</returns>
    public async Task<string?> LoginAsync(LoginForm form, HttpContext httpContext)
    {
        await using var scope = scopes.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUserLoginUseCase>();

        var request = new LoginRequest(
            form.Email.Trim(),
            form.Password,
            DeviceName.From(httpContext.Request.Headers.UserAgent),
            form.RememberMe);

        var result = await useCase.LoginAsync(request, httpContext.RequestAborted);

        if (result.IsError)
            return ToMessage(result.FirstError);

        await httpContext.SignInAsync(
            SessionClaims.CreatePrincipal(result.Value.UserId, result.Value.SessionId, result.Value.FullName),
            new AuthenticationProperties
            {
                IsPersistent = form.RememberMe,
                ExpiresUtc = new DateTimeOffset(result.Value.ExpiresAt)
            });

        return null;
    }

    /// <summary>
    /// A dónde ir tras iniciar sesión. Solo rutas locales: returnUrl viene en el enlace
    /// y sin esto alguien podría usar nuestro login para llevar al usuario a otro sitio.
    /// </summary>
    public static string RedirectTarget(string? returnUrl)
    {
        var isLocal = returnUrl is { Length: > 0 }
            && returnUrl[0] == '/'
            && !returnUrl.StartsWith("//")
            && !returnUrl.StartsWith("/\\");

        return isLocal ? returnUrl! : DefaultTarget;
    }

    private static string ToMessage(Error error) =>
        MessagesByCode.GetValueOrDefault(error.Code, "Ocurrió un error inesperado. Intenta de nuevo.");
}
