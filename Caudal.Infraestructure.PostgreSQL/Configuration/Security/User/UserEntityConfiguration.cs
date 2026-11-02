using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Caudal.Infraestructure.PostgreSQL.Configuration.Security;

public class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<User> builder)
    {
        builder.ToTable("app_user", "security", table =>
            table.HasCheckConstraint("ck_app_user_email_lower", "email = lower(email)"));

        builder.HasKey(user => user.UserId)
            .HasName("pk_app_user_id");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("uq_app_user_email");

        builder.Property(user => user.UserId)
            .HasColumnName("id")
            .HasConversion(id => id!.Value, value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(user => user.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(150);

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasConversion(email => email!.Value!, value => Email.Create(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.Password)
            .HasColumnName("password")
            .HasMaxLength(250);

        builder.Property(user => user.AccountCreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        builder.Property(user => user.SaltCode)
            .HasColumnName("salt_code")
            .HasConversion(saltCode => saltCode!.Value!, value => new SaltCode(value))
            .HasMaxLength(10);

        builder.Property(user => user.Status)
            .HasColumnName("status_id")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(user => user.EmailVerifiedAt)
            .HasColumnName("email_verified_at");

        builder.Property<short>("FailedLoginCount")
            .HasColumnName("failed_login_count")
            .IsRequired();

        builder.Property(user => user.AccountLockedUntil)
            .HasColumnName("locked_until");
    }
}
