using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Caudal.Infraestructure.PostgreSQL;

internal sealed class PostgreSQLDatabaseProvider : IDatabaseProvider
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyPostgreSQLConfigurations();
    }

    public bool TryGetUniqueConstraintName(DbUpdateException exception, out string? constraintName)
    {
        if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException)
        {
            constraintName = postgresException.ConstraintName;
            return true;
        }

        constraintName = null;
        return false;
    }
}
