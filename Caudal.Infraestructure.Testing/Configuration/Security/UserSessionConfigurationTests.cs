using Caudal.Domain.Security.Session.Entity;
using Caudal.Infraestructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Caudal.Infraestructure.Testing.Configuration.Security;

public class UserSessionConfigurationTests
{
    private static IEntityType SessionEntity()
    {
        var options = new DbContextOptionsBuilder<CaudalDbContext>()
            .UseNpgsql("Host=unused;Database=unused")
            .Options;
        using var context = new CaudalDbContext(options, new PostgreSQLDatabaseProvider());

        return context.Model.FindEntityType(typeof(UserSession))!;
    }

    // Bueno: la entidad coincide con la tabla security.app_user_session.

    [Fact]
    public void Model_MapsSessionToItsTable()
    {
        // Act
        var entity = SessionEntity();

        // Assert
        Assert.Equal("security", entity.GetSchema());
        Assert.Equal("app_user_session", entity.GetTableName());
    }

    [Theory]
    [InlineData(nameof(UserSession.SessionId), "id", false)]
    [InlineData(nameof(UserSession.UserId), "user_id", false)]
    [InlineData(nameof(UserSession.Device), "device", false)]
    [InlineData(nameof(UserSession.CreatedAt), "created_at", false)]
    [InlineData(nameof(UserSession.LastActivityAt), "last_activity_at", true)]
    [InlineData(nameof(UserSession.ExpiresAt), "expires_at", false)]
    public void Model_MapsPropertyToColumn(string propertyName, string expectedColumn, bool expectedNullable)
    {
        // Act
        var property = SessionEntity().FindProperty(propertyName)!;

        // Assert
        Assert.Equal(expectedColumn, property.GetColumnName());
        Assert.Equal(expectedNullable, property.IsNullable);
    }
}
