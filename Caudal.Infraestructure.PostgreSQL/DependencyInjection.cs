using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Infraestructure.PostgreSQL;

public static class DependencyInjection
{
    /// <summary>
    /// Registra lo que PostgreSQL aporta a la infraestructura: su mapeo de entidades
    /// y cómo reconoce una violación de clave única.
    /// </summary>
    public static IServiceCollection AddPostgreSQL(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseProvider, PostgreSQLDatabaseProvider>();

        return services;
    }

    /// <summary>
    /// Configura el DbContext para usar PostgreSQL con la cadena de conexión DefaultConnection.
    /// </summary>
    public static DbContextOptionsBuilder UsePostgreSQL(this DbContextOptionsBuilder options, IConfiguration configuration)
    {
        return options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
    }
}
