using Caudal.Application.Services.AppUser.CreateAccount;
using Caudal.Domain.Security.Service;
using Caudal.Infraestructure.PostgreSQL;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
            .AddScoped(_ => Mock.Of<IAccountNotifier>())
            .AddPostgreSQL()
            .AddApplication(options => options.UsePostgreSQL(configuration))
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
