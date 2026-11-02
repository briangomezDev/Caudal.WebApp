using Caudal.Domain.Security.Entity;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure;

internal interface IApplicationDbContext
{
    public DbSet<User>? Users { get; set; }
}
