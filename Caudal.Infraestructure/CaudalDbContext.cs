using Caudal.Domain.Security.Entity;
using Caudal.Infraestructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure;

internal sealed class CaudalDbContext : DbContext, IApplicationDbContext
{
    public DbSet<User>? Users { get; set; }

    public CaudalDbContext(DbContextOptions<CaudalDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyPostgreSQLConfigurations();
    }
}
