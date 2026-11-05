using Caudal.Domain.Security.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Infraestructure.Mailing;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el correo como canal de notificaciones. Otro canal (WhatsApp, SMS) sería
    /// otro proyecto con su propia implementación de IAccountNotifier.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection("Email"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Email:Host is required")
            .Validate(options => !string.IsNullOrWhiteSpace(options.FromAddress), "Email:FromAddress is required")
            .ValidateOnStart();

        services.AddSingleton<EmailTemplates>();
        services.AddScoped<IEmailTransport, MailKitEmailTransport>();
        services.AddScoped<IAccountNotifier, EmailAccountNotifier>();

        return services;
    }
}
