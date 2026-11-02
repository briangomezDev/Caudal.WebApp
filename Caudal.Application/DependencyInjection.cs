using Caudal.Application.Services.AppUser.CreateAccount;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los casos de uso. La infraestructura la registra el proveedor
    /// de base de datos (por ejemplo AddPostgreSQL) desde el proyecto web.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        service.AddValidators();
        service.AddScoped<IUserCreateAccountUseCase, UserCreateAccountUseCase>();
        return service;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<UserRequest>, UserRequestValidator>();
        return services;
    }
}
