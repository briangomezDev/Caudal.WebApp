using Caudal.BlazorApp.Modules.Account.Login;
using Caudal.BlazorApp.Modules.Account.Register;
using Caudal.BlazorApp.Modules.Account.Session;
using Microsoft.AspNetCore.Components.Authorization;

namespace Caudal.BlazorApp.Modules.Account;

public static class AccountModule
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddScoped<RegisterFacade>();
        services.AddScoped<LoginFacade>();
        services.AddScoped<SessionFacade>();
        services.AddScoped<SessionCookieEvents>();
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, SessionRevalidationProvider>();
        services.AddLoginRateLimit();
        return services;
    }
}
