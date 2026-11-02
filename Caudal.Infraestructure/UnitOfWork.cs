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
    
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
    
}
