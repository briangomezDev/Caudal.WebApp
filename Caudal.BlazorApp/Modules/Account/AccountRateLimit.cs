using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Caudal.BlazorApp.Modules.Account;

/// <summary>
/// Límite por IP para los formularios sensibles de la cuenta. En el login frena a quien prueba muchas
/// cuentas y protege la CPU; en el reenvío evita usar la app para llenar de correos a un tercero.
/// Detrás de un proxy hay que configurar ForwardedHeaders, o todos los usuarios contarán como una sola IP.
/// </summary>
public static class AccountRateLimit
{
    public const string ThrottledReason = "throttled";

    private static readonly Dictionary<string, int> AttemptsPerMinuteByPath = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/account/login"] = 10,
        ["/account/resend-confirmation"] = 3,
    };

    public static IServiceCollection AddAccountRateLimit(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var path = context.Request.Path.Value ?? string.Empty;

                if (!HttpMethods.IsPost(context.Request.Method) || !AttemptsPerMinuteByPath.TryGetValue(path, out var attempts))
                    return RateLimitPartition.GetNoLimiter(string.Empty);

                return RateLimitPartition.GetFixedWindowLimiter(
                    $"{path.ToLowerInvariant()}|{context.Connection.RemoteIpAddress}",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = attempts, Window = TimeSpan.FromMinutes(1) });
            });

            options.OnRejected = OnRejected;
        });
    }

    // De vuelta al formulario con una explicación, conservando la query (por ejemplo, a dónde iba el usuario).
    private static ValueTask OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var request = context.HttpContext.Request;
        var target = QueryHelpers.AddQueryString($"{request.Path}{request.QueryString}", "reason", ThrottledReason);

        context.HttpContext.Response.Redirect(target);

        return ValueTask.CompletedTask;
    }
}
