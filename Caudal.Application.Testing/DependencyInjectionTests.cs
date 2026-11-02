using Caudal.Application.Services.AppUser.CreateAccount;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Caudal.Application.Testing;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_WithConfiguration_ResolvesServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=unused;Database=unused"
            })
            .Build();
        using var services = new ServiceCollection()
            .AddLogging()
            .AddApplication(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = services.CreateScope();

        // Act
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<UserRequest>>();
        var useCase = scope.ServiceProvider.GetRequiredService<IUserCreateAccountUseCase>();

        // Assert
        Assert.IsType<UserRequestValidator>(validator);
        Assert.IsType<UserCreateAccountUseCase>(useCase);
    }
}
