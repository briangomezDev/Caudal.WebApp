using Caudal.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly CaudalDbContext _dbContext;
    private readonly IDatabaseProvider _databaseProvider;

    public UnitOfWork(CaudalDbContext dbContext, IDatabaseProvider databaseProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
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
        catch (DbUpdateException ex) when (_databaseProvider.TryGetUniqueConstraintName(ex, out var constraintName))
        {
            throw new UniqueConstraintException(constraintName, "A unique constraint was violated.");
        }
    }
}
