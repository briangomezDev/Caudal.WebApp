using Caudal.Application.Services.AppUser.CreateAccount;
using Caudal.BlazorApp.Modules.Account.Register;
using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Caudal.BlazorApp.Testing.Modules.Account.Register;

public class RegisterFacadeTests
{
    private readonly Mock<IUserCreateAccountUseCase> _useCase = new();

    private RegisterFacade CreateFacade()
    {
        var services = new ServiceCollection()
            .AddScoped(_ => _useCase.Object)
            .BuildServiceProvider();

        return new RegisterFacade(services.GetRequiredService<IServiceScopeFactory>(), new UserRequestValidator());
    }

    private static RegisterForm ValidForm() => new()
    {
        FullName = "  Brian Gomez  ",
        Email = "  brian@caudal.com  ",
        Password = "Correct-password1",
        Terms = true
    };

    private void UseCaseReturns(ErrorOr<bool> result) =>
        _useCase
            .Setup(u => u.CreateAccountAsync(It.IsAny<UserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    // Bueno: el flujo esperado se completa correctamente.

    [Fact]
    public void Validate_WithValidForm_ReturnsNoErrors()
    {
        // Act
        var errors = CreateFacade().Validate(ValidForm());

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RegisterAsync_WithValidForm_SendsTrimmedRequestAndSucceeds()
    {
        // Arrange
        UseCaseReturns(true);

        // Act
        var result = await CreateFacade().RegisterAsync(ValidForm(), CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        _useCase.Verify(u => u.CreateAccountAsync(
            new UserRequest("Brian Gomez", "brian@caudal.com", "Correct-password1"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_CalledTwice_ResolvesUseCaseInSeparateScopes()
    {
        // Arrange
        var scopes = new List<IServiceProvider>();
        UseCaseReturns(true);
        var services = new ServiceCollection()
            .AddScoped(provider =>
            {
                scopes.Add(provider);
                return _useCase.Object;
            })
            .BuildServiceProvider();
        var facade = new RegisterFacade(services.GetRequiredService<IServiceScopeFactory>(), new UserRequestValidator());

        // Act
        await facade.RegisterAsync(ValidForm(), CancellationToken.None);
        await facade.RegisterAsync(ValidForm(), CancellationToken.None);

        // Assert
        Assert.Equal(2, scopes.Count);
        Assert.NotSame(scopes[0], scopes[1]);
    }

    // Malo: cada error llega traducido a su campo o como mensaje general.

    [Theory]
    [InlineData("", "brian@caudal.com", "Correct-password1", RegisterField.FullName, "El nombre es requerido")]
    [InlineData("Brian", "abc", "Correct-password1", RegisterField.Email, "El correo no es válido")]
    [InlineData("Brian", "brian@caudal.com", "Abc1234", RegisterField.Password, "La contraseña debe tener al menos 8 caracteres")]
    [InlineData("Brian", "brian@caudal.com", "Aaaaaaaa", RegisterField.Password, "La contraseña debe incluir un número")]
    [InlineData("Brian", "brian@caudal.com", "aaaaaaa1", RegisterField.Password, "La contraseña debe incluir una mayúscula")]
    public void Validate_WithInvalidField_ReturnsMessageInSpanish(
        string fullName,
        string email,
        string password,
        RegisterField expectedField,
        string expectedMessage)
    {
        // Arrange
        var form = new RegisterForm { FullName = fullName, Email = email, Password = password, Terms = true };

        // Act
        var errors = CreateFacade().Validate(form);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal(expectedField, error.Key);
        Assert.Equal(expectedMessage, error.Value);
    }

    [Fact]
    public async Task RegisterAsync_WithoutTerms_ReturnsErrorAndDoesNotCallUseCase()
    {
        // Arrange
        var form = ValidForm();
        form.Terms = false;

        // Act
        var result = await CreateFacade().RegisterAsync(form, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(RegisterField.Terms, result.FieldErrors.Keys);
        _useCase.Verify(u => u.CreateAccountAsync(It.IsAny<UserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsErrorOnEmailField()
    {
        // Arrange
        UseCaseReturns(Error.Conflict("SECURITY-0012", "User already exists with this email"));

        // Act
        var result = await CreateFacade().RegisterAsync(ValidForm(), CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Ya existe una cuenta con este correo", result.FieldErrors[RegisterField.Email]);
        Assert.Equal("Revisa los campos marcados", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_WhenUseCaseFails_ReturnsGeneralMessage()
    {
        // Arrange
        UseCaseReturns(Error.Failure("SECURITY-0011", "An unexpected error occurred"));

        // Act
        var result = await CreateFacade().RegisterAsync(ValidForm(), CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.FieldErrors);
        Assert.Equal("Ocurrió un error inesperado. Intenta de nuevo.", result.Message);
    }
}
