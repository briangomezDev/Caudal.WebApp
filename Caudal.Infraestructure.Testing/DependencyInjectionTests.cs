using Caudal.Domain.Security.Adapter;
using Caudal.Domain.Security.Service;
using Caudal.Infraestructure.Port.Security;
using Caudal.Infraestructure.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Caudal.Infraestructure.Testing;

public class DependencyInjectionTests
{
    private static ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=unused;Database=unused"
            })
            .Build();

        return new ServiceCollection()
            .AddPostgreSQL(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    // Bueno: el flujo esperado se completa correctamente.

    [Fact]
    public void AddInfrastructure_WithConfiguration_ResolvesServices()
    {
        // Arrange
        using var services = CreateServices();
        using var scope = services.CreateScope();

        // Act
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Assert
        Assert.IsType<UnitOfWork>(unitOfWork);
        Assert.IsType<UserRepository>(repository);
    }

    [Fact]
    public void AddInfrastructure_WithConfiguration_ResolvesSecurityServices()
    {
        // Arrange
        using var services = CreateServices();
        using var scope = services.CreateScope();

        // Act
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var saltCodeGenerator = scope.ServiceProvider.GetRequiredService<ISaltCodeGenerator>();

        // Assert
        Assert.IsType<PasswordHasher>(passwordHasher);
        Assert.IsType<CryptographySaltCodeGenerator>(saltCodeGenerator);
    }

    [Fact]
    public void AddInfrastructure_InSameScope_SharesDbContext()
    {
        // Arrange
        using var services = CreateServices();
        using var scope = services.CreateScope();

        // Act
        var context = scope.ServiceProvider.GetRequiredService<CaudalDbContext>();
        var applicationContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Assert
        Assert.Same(context, applicationContext);
    }

    // Malo: los servicios con alcance no se pueden pedir fuera de un scope.

    [Fact]
    public void AddInfrastructure_WithoutScope_ThrowsException()
    {
        // Arrange
        using var services = CreateServices();

        // Act
        var act = () => services.GetRequiredService<IUnitOfWork>();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}
