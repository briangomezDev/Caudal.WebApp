using Caudal.Domain.Security.Session.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Caudal.Infraestructure.PostgreSQL.Configuration.Security;

internal class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("app_user_session","security");

        builder.HasKey(x => x.SessionId)
        .HasName("pk_app_user_session");

        
    }
}
