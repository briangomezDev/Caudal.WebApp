using Caudal.Domain.Security.Session.Exception;
using Caudal.Domain.Security.Session.ValueObjects;
using Xunit;

namespace Caudal.Domain.Testing.Security.Session;

public class SessionIdTests
{
    // Bueno: un identificador con valor se crea correctamente.

    [Fact]
    public void Constructor_WithValue_KeepsValue()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        var sessionId = new SessionId(value);

        // Assert
        Assert.Equal(value, sessionId.Value);
    }

    // Malo: un identificador vacío se rechaza.

    [Fact]
    public void Constructor_WithEmptyValue_ThrowsException()
    {
        // Act
        var act = () => new SessionId(Guid.Empty);

        // Assert
        Assert.Throws<SessionInvalidIdException>(act);
    }
}
