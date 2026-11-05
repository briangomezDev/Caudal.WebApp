using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Caudal.BlazorApp.Modules.Account.Login;

/// <summary>
/// Límite por IP para los intentos de login. Complementa el bloqueo por cuenta: frena a quien prueba
/// muchas cuentas distintas y protege la CPU, porque cada intento calcula un hash costoso.
/// Detrás de un proxy hay que configurar ForwardedHeaders, o todos los usuarios contarán como una sola IP.
/// </summary>
public static class LoginRateLimit
{
    public const string ThrottledReason = "throttled";

    private const string LoginPath = "/account/login";
    private const int AttemptsPerMinute = 10;

    public static IServiceCollection AddLoginRateLimit(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsLoginAttempt(context.Request)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = AttemptsPerMinute,
                            Window = TimeSpan.FromMinutes(1)
                        })
                    : RateLimitPartition.GetNoLimiter(string.Empty));

            options.OnRejected = OnRejected;
        });
    }

    private static bool IsLoginAttempt(HttpRequest request) =>
        HttpMethods.IsPost(request.Method)
        && request.Path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase);

    // De vuelta al formulario con una explicación, conservando a dónde iba el usuario.
    private static ValueTask OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var request = context.HttpContext.Request;
        var target = QueryHelpers.AddQueryString(LoginPath + request.QueryString, "reason", ThrottledReason);

        context.HttpContext.Response.Redirect(target);

        return ValueTask.CompletedTask;
    }
}
