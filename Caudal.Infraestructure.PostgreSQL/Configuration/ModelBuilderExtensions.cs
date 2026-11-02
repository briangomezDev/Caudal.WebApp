using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure.PostgreSQL.Configuration;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyPostgreSQLConfigurations(this ModelBuilder modelBuilder)
    {
        return modelBuilder.ApplyConfigurationsFromAssembly(typeof(ModelBuilderExtensions).Assembly);
    }
}
