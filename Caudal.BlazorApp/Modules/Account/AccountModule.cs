using Caudal.BlazorApp.Modules.Account.Register;

namespace Caudal.BlazorApp.Modules.Account;

public static class AccountModule
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddScoped<RegisterFacade>();
        return services;
    }
}
