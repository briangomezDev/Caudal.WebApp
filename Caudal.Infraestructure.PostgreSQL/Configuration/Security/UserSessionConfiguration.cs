using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;
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

        builder.Property(x => x.SessionId)
            .HasColumnName("id")
            .HasConversion(id => id!.Value, value => new SessionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id!.Value, value => new UserId(value))
            .IsRequired();

        // La columna se llama así en la base de datos.
        builder.Property(x => x.Device)
            .HasColumnName("devide")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.LastActivityAt)
            .HasColumnName("last_activity_at");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
    }
}
