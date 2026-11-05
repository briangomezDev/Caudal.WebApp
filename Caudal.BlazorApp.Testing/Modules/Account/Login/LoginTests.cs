using Caudal.Application.Services.AppUser.Login;
using Caudal.BlazorApp.Modules.Account.Login;
using ErrorOr;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Caudal.BlazorApp.Testing.Modules.Account.Login;

public class LoginTests
{
    private const string FirefoxOnLinux = "Mozilla/5.0 (X11; Linux x86_64; rv:150.0) Gecko/20100101 Firefox/150.0";

    private readonly Mock<IUserLoginUseCase> _useCase = new();
    private readonly Mock<IAuthenticationService> _authentication = new();

    private (LoginFacade Facade, HttpContext Context) Create()
    {
        var services = new ServiceCollection()
            .AddScoped(_ => _useCase.Object)
            .AddSingleton(_authentication.Object)
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Headers.UserAgent = FirefoxOnLinux;

        return (new LoginFacade(services.GetRequiredService<IServiceScopeFactory>()), context);
    }

    // Bueno: una sesión registrada queda en la cookie.

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoginAsync_WithValidCredentials_SignsInUntilSessionExpires(bool rememberMe)
    {
        // Arrange
        var expiresAt = new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);
        _useCase
            .Setup(u => u.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse(Guid.NewGuid(), Guid.NewGuid(), "Brian", expiresAt));
        var (facade, context) = Create();

        // Act
        var error = await facade.LoginAsync(new LoginForm { Email = " brian@caudal.com ", Password = "secret", RememberMe = rememberMe }, context);

        // Assert
        Assert.Null(error);
        _useCase.Verify(u => u.LoginAsync(new LoginRequest("brian@caudal.com", "secret", "Firefox en Linux", rememberMe), It.IsAny<CancellationToken>()), Times.Once);
        _authentication.Verify(a => a.SignInAsync(
            context,
            null,
            It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
            It.Is<AuthenticationProperties>(p => p.IsPersistent == rememberMe && p.ExpiresUtc == new DateTimeOffset(expiresAt))), Times.Once);
    }

    [Theory]
    [InlineData("/", "/")]
    [InlineData("/transactions?q=1", "/transactions?q=1")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    public void RedirectTarget_WithLocalPath_KeepsIt(string? returnUrl, string expected)
    {
        Assert.Equal(expected, LoginFacade.RedirectTarget(returnUrl));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64; rv:150.0) Gecko/20100101 Firefox/150.0", "Firefox en Linux")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/140.0 Safari/537.36", "Chrome en Windows")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/140.0 Safari/537.36 Edg/140.0", "Edge en Windows")]
    [InlineData("Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/140.0 Mobile Safari/537.36", "Chrome en Android")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Safari/604.1", "Safari en iOS")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/605.1.15 Version/18.0 Safari/605.1.15", "Safari en macOS")]
    [InlineData("curl/8.5", "Dispositivo desconocido")]
    [InlineData(null, "Dispositivo desconocido")]
    public void DeviceName_FromUserAgent_ReturnsReadableName(string? userAgent, string expected)
    {
        Assert.Equal(expected, DeviceName.From(userAgent));
    }

    // Malo: los errores llegan en español y no se inicia sesión.

    [Theory]
    [InlineData("SECURITY-0015", "Correo o contraseña incorrectos. Tras varios intentos fallidos, la cuenta se bloquea 15 minutos.")]
    [InlineData("SECURITY-0016", "Tu cuenta no está disponible. Contacta a soporte.")]
    [InlineData("SECURITY-0011", "Ocurrió un error inesperado. Intenta de nuevo.")]
    public async Task LoginAsync_WhenUseCaseFails_ReturnsMessageAndDoesNotSignIn(string code, string expected)
    {
        // Arrange
        _useCase
            .Setup(u => u.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure(code, "server message"));
        var (facade, context) = Create();

        // Act
        var error = await facade.LoginAsync(new LoginForm { Email = "a@b.com", Password = "x" }, context);

        // Assert
        Assert.Equal(expected, error);
        _authentication.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Theory]
    [InlineData("https://malicioso.example/")]
    [InlineData("//malicioso.example/")]
    [InlineData("/\\malicioso.example/")]
    [InlineData("javascript:alert(1)")]
    public void RedirectTarget_WithExternalUrl_GoesToSummary(string returnUrl)
    {
        Assert.Equal("/", LoginFacade.RedirectTarget(returnUrl));
    }
}
