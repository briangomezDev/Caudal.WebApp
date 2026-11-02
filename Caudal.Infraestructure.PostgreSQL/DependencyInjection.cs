using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Infraestructure.PostgreSQL;

public static class DependencyInjection
{
    /// <summary>
    /// Registra la infraestructura usando PostgreSQL como motor.
    /// Es la única línea que la aplicación necesita para usar este proveedor.
    /// </summary>
    public static IServiceCollection AddPostgreSQL(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDatabaseProvider, PostgreSQLDatabaseProvider>();

        services.AddInfrastructure(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
