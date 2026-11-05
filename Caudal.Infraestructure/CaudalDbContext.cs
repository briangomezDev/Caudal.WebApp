using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Session.Entity;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure;

internal sealed class CaudalDbContext : DbContext, IApplicationDbContext
{
    private readonly IDatabaseProvider _databaseProvider;

    public DbSet<User>? Users { get; set; }
    public DbSet<UserSession>? UserSessions { get; set; }

    public CaudalDbContext(DbContextOptions<CaudalDbContext> options, IDatabaseProvider databaseProvider) : base(options)
    {
        _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _databaseProvider.ConfigureModel(modelBuilder);
    }
}
