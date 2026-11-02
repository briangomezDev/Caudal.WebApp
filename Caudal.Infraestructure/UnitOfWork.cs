using Caudal.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Caudal.Infraestructure;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly CaudalDbContext _dbContext;
    public UnitOfWork(CaudalDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    public Task BeginTransactionAsync() => _dbContext.Database.BeginTransactionAsync();
    
    public Task CommitTransactionAsync() =>_dbContext.Database.CommitTransactionAsync();

    public Task RollbackTransactionAsync() => _dbContext.Database.RollbackTransactionAsync();
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException)
        {
            throw new UniqueConstraintException(postgresException.ConstraintName, "A unique constraint was violated.");
        }
    }
}
