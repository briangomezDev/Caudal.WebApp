using Caudal.Domain.Security.ValueObjects;
using Caudal.Infraestructure.Port.Security;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Caudal.Infraestructure.Testing.Port.Security;

public class DataProtectionAccountConfirmationTokensTests
{
    private readonly IDataProtectionProvider _provider = new EphemeralDataProtectionProvider();

    private DataProtectionAccountConfirmationTokens CreateTokens() => new(_provider);

    // Bueno: el token conserva a quién pertenece y se puede poner en una URL.

    [Fact]
    public void Create_ThenRead_ReturnsSameUserAndEmail()
    {
        // Arrange
        var userId = new UserId(Guid.NewGuid());
        var email = Email.Create("brian@caudal.com");
        var tokens = CreateTokens();

        // Act
        var read = tokens.TryRead(tokens.Create(userId, email), out var claim);

        // Assert
        Assert.True(read);
        Assert.Equal(userId, claim!.UserId);
        Assert.Equal(email, claim.Email);
    }

    [Fact]
    public void Create_ReturnsUrlSafeToken()
    {
        // Act
        var token = CreateTokens().Create(new UserId(Guid.NewGuid()), Email.Create("brian@caudal.com"));

        // Assert
        Assert.Equal(token, Uri.EscapeDataString(token));
    }

    // Malo: un token alterado, vencido o de otra clave no vale.

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-token")]
    public void TryRead_WithGarbage_ReturnsFalse(string token)
    {
        Assert.False(CreateTokens().TryRead(token, out _));
    }

    [Fact]
    public void TryRead_WithTamperedToken_ReturnsFalse()
    {
        // Arrange
        var tokens = CreateTokens();
        var token = tokens.Create(new UserId(Guid.NewGuid()), Email.Create("brian@caudal.com"));

        // Act
        var read = tokens.TryRead(token[..^4] + "AAAA", out _);

        // Assert
        Assert.False(read);
    }

    [Fact]
    public void TryRead_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var expired = _provider
            .CreateProtector(DataProtectionAccountConfirmationTokens.Purpose)
            .ToTimeLimitedDataProtector()
            .Protect($"{Guid.NewGuid()}|brian@caudal.com", TimeSpan.FromSeconds(-1));

        // Act
        var read = CreateTokens().TryRead(expired, out _);

        // Assert
        Assert.False(read);
    }

    [Fact]
    public void TryRead_WithTokenFromAnotherKey_ReturnsFalse()
    {
        // Arrange
        var foreign = new DataProtectionAccountConfirmationTokens(new EphemeralDataProtectionProvider())
            .Create(new UserId(Guid.NewGuid()), Email.Create("brian@caudal.com"));

        // Act
        var read = CreateTokens().TryRead(foreign, out _);

        // Assert
        Assert.False(read);
    }
}
