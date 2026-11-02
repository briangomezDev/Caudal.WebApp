using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Service;
using Caudal.Infraestructure.Port.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CaudalDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

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
