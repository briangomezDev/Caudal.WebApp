using Caudal.Application.Services.AppUser.CreateAccount;
using Caudal.Infraestructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los casos de uso y la infraestructura. El proyecto web elige el motor
    /// con configureDatabase y registra su proveedor (por ejemplo AddPostgreSQL).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection service, Action<DbContextOptionsBuilder> configureDatabase)
    {
        service.AddValidators();
        service.AddInfrastructure(configureDatabase);
        service.AddScoped<IUserCreateAccountUseCase, UserCreateAccountUseCase>();
        return service;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<UserRequest>, UserRequestValidator>();
        return services;
    }
}
