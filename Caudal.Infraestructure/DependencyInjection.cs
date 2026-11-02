using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Service;
using Caudal.Infraestructure.Port.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Infraestructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra la infraestructura sin depender de ningún motor.
    /// La llama AddApplication con la conexión que recibe del proyecto web. La implementación
    /// de IDatabaseProvider la registra el proyecto del proveedor (por ejemplo AddPostgreSQL).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDatabase)
    {
        ArgumentNullException.ThrowIfNull(configureDatabase);

        services.AddDbContext<CaudalDbContext>(configureDatabase);

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<CaudalDbContext>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddPorts();

        return services;
    }

    private static IServiceCollection AddPorts(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISaltCodeGenerator, CryptographySaltCodeGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        return services;
    }
}
