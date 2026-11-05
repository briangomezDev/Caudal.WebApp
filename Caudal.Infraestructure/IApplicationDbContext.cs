using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Session.Entity;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure;

internal interface IApplicationDbContext
{
    public DbSet<User>? Users { get; set; }
    public DbSet<UserSession>? UserSessions { get; set; }
}
