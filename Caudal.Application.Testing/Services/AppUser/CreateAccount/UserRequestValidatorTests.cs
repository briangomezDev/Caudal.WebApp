using Caudal.Application.Services.AppUser.CreateAccount;
using Xunit;

namespace Caudal.Application.Testing.Services.AppUser.CreateAccount;

public class UserRequestValidatorTests
{
    private const string ValidName = "Brian Gomez";
    private const string ValidEmail = "brian@caudal.com";
    private const string ValidPassword = "Correct-password1";

    public static TheoryData<string?, string?, string?, string> InvalidRequests => new()
    {
        { null, ValidEmail, ValidPassword, "SECURITY-0001" },
        { "", ValidEmail, ValidPassword, "SECURITY-0001" },
        { "   ", ValidEmail, ValidPassword, "SECURITY-0001" },
        { new string('a', 151), ValidEmail, ValidPassword, "SECURITY-0002" },
        { ValidName, null, ValidPassword, "SECURITY-0003" },
        { ValidName, "", ValidPassword, "SECURITY-0003" },
        { ValidName, new string('a', 90) + "@caudal.com", ValidPassword, "SECURITY-0004" },
        { ValidName, "brian.caudal.com", ValidPassword, "SECURITY-0005" },
        { ValidName, ValidEmail, null, "SECURITY-0006" },
        { ValidName, ValidEmail, "", "SECURITY-0006" },
        { ValidName, ValidEmail, new string('a', 7), "SECURITY-0007" },
        { ValidName, ValidEmail, new string('a', 129), "SECURITY-0009" },
        { ValidName, ValidEmail, "Aaaaaaaa", "SECURITY-0013" },
        { ValidName, ValidEmail, "aaaaaaa1", "SECURITY-0014" }
    };

    public static TheoryData<string, string, string> BoundaryRequests => new()
    {
        { new string('a', 150), ValidEmail, ValidPassword },
        { ValidName, new string('a', 89) + "@caudal.com", ValidPassword },
        { ValidName, ValidEmail, "Aaaaaaa1" },
        { ValidName, ValidEmail, "A1" + new string('a', 126) },
        { ValidName, ValidEmail, "Ñaaaaaa1" }
    };

    // Bueno: las solicitudes válidas se aceptan.

    [Fact]
    public async Task ValidateAsync_WithValidRequest_ReturnsValid()
    {
        // Arrange
        var validator = new UserRequestValidator();
        var request = new UserRequest(ValidName, ValidEmail, ValidPassword);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [MemberData(nameof(BoundaryRequests))]
    public async Task ValidateAsync_WithValuesAtLimit_ReturnsValid(string fullName, string email, string password)
    {
        // Arrange
        var validator = new UserRequestValidator();
        var request = new UserRequest(fullName, email, password);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    // Malo: cada regla devuelve un único error con su código.

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task ValidateAsync_WithInvalidField_ReturnsSingleErrorWithCode(
        string? fullName,
        string? email,
        string? password,
        string expectedCode)
    {
        // Arrange
        var validator = new UserRequestValidator();
        var request = new UserRequest(fullName!, email!, password!);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        var error = Assert.Single(result.Errors);
        Assert.Equal(expectedCode, error.ErrorCode);
    }

    [Fact]
    public async Task ValidateAsync_WithAllFieldsNull_ReturnsOneErrorPerField()
    {
        // Arrange
        var validator = new UserRequestValidator();
        var request = new UserRequest(null!, null!, null!);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        Assert.Equal(
            ["SECURITY-0001", "SECURITY-0003", "SECURITY-0006"],
            result.Errors.Select(error => error.ErrorCode));
    }

    // Fallo: el validador no puede ejecutarse sin una solicitud.

    [Fact]
    public async Task ValidateAsync_WithNullRequest_ThrowsException()
    {
        // Arrange
        var validator = new UserRequestValidator();
        UserRequest request = null!;

        // Act
        var act = () => validator.ValidateAsync(request);

        // Assert
        await Assert.ThrowsAnyAsync<Exception>(act);
    }
}
