using Caudal.Infraestructure.Port.Security;
using Xunit;

namespace Caudal.Infraestructure.Testing.Port.Security;

public class CryptographySaltCodeGeneratorTests
{
    [Fact]
    public void Generate_WhenCalled_ReturnsTenAlphanumericCharacters()
    {
        // Arrange
        var generator = new CryptographySaltCodeGenerator();

        // Act
        var saltCode = generator.Generate();

        // Assert
        Assert.Equal(10, saltCode.Value!.Length);
        Assert.All(saltCode.Value, character => Assert.True(char.IsAsciiLetterOrDigit(character)));
    }

    [Fact]
    public void Generate_WhenCalledRepeatedly_ReturnsDifferentValues()
    {
        // Arrange
        var generator = new CryptographySaltCodeGenerator();

        // Act
        var saltCodes = Enumerable.Range(0, 1000).Select(_ => generator.Generate().Value).ToHashSet();

        // Assert
        Assert.Equal(1000, saltCodes.Count);
    }
}
