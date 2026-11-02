using Caudal.Domain.Security.Service;
using Caudal.Domain.Security.ValueObjects;
using Caudal.Infraestructure.Port.Security;
using Moq;
using Xunit;

namespace Caudal.Infraestructure.Testing.Port.Security;

public class PasswordHasherTests
{
    private const string Password = "correct-password";

    private static PasswordHasher CreateHasher(string salt = "abc123")
    {
        var saltCodeGenerator = new Mock<ISaltCodeGenerator>();
        saltCodeGenerator.Setup(g => g.Generate()).Returns(new SaltCode(salt));

        return new PasswordHasher(saltCodeGenerator.Object);
    }

    // Bueno: el flujo esperado se completa correctamente.

    [Fact]
    public void Hash_WithValidPassword_ReturnsGeneratedSaltAndHashThatFitsColumn()
    {
        // Arrange
        var hasher = CreateHasher("abc123");

        // Act
        var (hash, salt) = hasher.Hash(Password);

        // Assert
        Assert.Equal(new SaltCode("abc123"), salt);
        Assert.DoesNotContain(Password, hash);
        Assert.InRange(hash.Length, 1, 250);
    }

    [Fact]
    public void Hash_WithDifferentSalts_ReturnsDifferentHashes()
    {
        // Arrange
        var firstHasher = CreateHasher("abc123");
        var secondHasher = CreateHasher("xyz789");

        // Act
        var (firstHash, _) = firstHasher.Hash(Password);
        var (secondHash, _) = secondHasher.Hash(Password);

        // Assert
        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var hasher = CreateHasher();
        var (hash, salt) = hasher.Hash(Password);

        // Act
        var isValid = hasher.Verify(Password, hash, salt);

        // Assert
        Assert.True(isValid);
    }

    // Malo: los datos incorrectos o inválidos se rechazan.

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var hasher = CreateHasher();
        var (hash, salt) = hasher.Hash(Password);

        // Act
        var isValid = hasher.Verify("wrong-password", hash, salt);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Verify_WithWrongSalt_ReturnsFalse()
    {
        // Arrange
        var hasher = CreateHasher();
        var (hash, _) = hasher.Hash(Password);

        // Act
        var isValid = hasher.Verify(Password, hash, new SaltCode("xyz789"));

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("abc.AAAA")]
    [InlineData("0.AAAA")]
    [InlineData("-1.AAAA")]
    [InlineData("1000.not-base64")]
    [InlineData("1000.AAAA")]
    [InlineData("1000.AAAA.AAAA")]
    public void Verify_WithMalformedHash_ReturnsFalse(string hash)
    {
        // Arrange
        var hasher = CreateHasher();

        // Act
        var isValid = hasher.Verify(Password, hash, new SaltCode("abc123"));

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_WithoutPassword_ThrowsException(string? password)
    {
        // Arrange
        var hasher = CreateHasher();

        // Act
        Action act = () => hasher.Hash(password!);

        // Assert
        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WithNullGenerator_ThrowsException()
    {
        // Arrange
        ISaltCodeGenerator saltCodeGenerator = null!;

        // Act
        var act = () => new PasswordHasher(saltCodeGenerator);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    // Fallo: una dependencia falla de forma inesperada.

    [Fact]
    public void Verify_WithNullPassword_ThrowsException()
    {
        // Arrange
        var hasher = CreateHasher();
        var (hash, salt) = hasher.Hash(Password);

        // Act
        Action act = () => hasher.Verify(null!, hash, salt);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Hash_WhenSaltGeneratorFails_PropagatesException()
    {
        // Arrange
        var saltCodeGenerator = new Mock<ISaltCodeGenerator>();
        saltCodeGenerator
            .Setup(g => g.Generate())
            .Throws(new InvalidOperationException("Random source unavailable."));
        var hasher = new PasswordHasher(saltCodeGenerator.Object);

        // Act
        Action act = () => hasher.Hash(Password);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}
