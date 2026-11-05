using Caudal.Domain.Security.Session.Entity;
using Caudal.Domain.Security.Session.Exception;
using Caudal.Domain.Security.Session.ValueObjects;
using Caudal.Domain.Security.ValueObjects;
using Xunit;

namespace Caudal.Domain.Testing.Security.Session;

public class UserSessionTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    private static UserSession CreateSession(string device = "Firefox en Linux", DateTime? expiresAt = null) =>
        new(new SessionId(Guid.NewGuid()), new UserId(Guid.NewGuid()), device, Now, expiresAt ?? Now.AddHours(8));

    // Bueno: la sesión nace completa y su duración se conserva al renovarla.

    [Fact]
    public void Constructor_WithValidData_SetsDeviceAndTimes()
    {
        // Act
        var session = CreateSession();

        // Assert
        Assert.Equal("Firefox en Linux", session.Device);
        Assert.Equal(Now, session.CreatedAt);
        Assert.Equal(Now, session.LastActivityAt);
        Assert.Equal(Now.AddHours(8), session.ExpiresAt);
    }

    [Fact]
    public void Renew_KeepsOriginalDuration()
    {
        // Arrange
        var session = CreateSession();
        var later = Now.AddHours(2);

        // Act
        session.Renew(later);

        // Assert
        Assert.Equal(later, session.LastActivityAt);
        Assert.Equal(later.AddHours(8), session.ExpiresAt);
    }

    [Fact]
    public void IsActive_BeforeExpiration_ReturnsTrue()
    {
        Assert.True(CreateSession().IsActive(Now.AddHours(7)));
    }

    // Malo: una sesión vencida o terminada deja de estar activa, y los datos inválidos se rechazan.

    [Fact]
    public void IsActive_AtExpiration_ReturnsFalse()
    {
        Assert.False(CreateSession().IsActive(Now.AddHours(8)));
    }

    [Fact]
    public void Expire_EndsTheSessionImmediately()
    {
        // Arrange
        var session = CreateSession();

        // Act
        session.Expire(Now.AddHours(1));

        // Assert
        Assert.False(session.IsActive(Now.AddHours(1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutDevice_ThrowsException(string device)
    {
        Assert.Throws<SessionInvalidDeviceException>(() => CreateSession(device));
    }

    [Fact]
    public void Constructor_WithTooLongDevice_ThrowsException()
    {
        Assert.Throws<SessionInvalidDeviceException>(() => CreateSession(new string('a', UserSession.DeviceMaxLength + 1)));
    }

    [Fact]
    public void Constructor_WithExpirationBeforeCreation_ThrowsException()
    {
        Assert.Throws<SessionInvalidExpiresAtException>(() => CreateSession(expiresAt: Now.AddMinutes(-1)));
    }
}
