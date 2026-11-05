using Caudal.Application.Services.AppUser.Confirmation;
using Caudal.Application.Services.AppUser.CreateAccount;
using Caudal.Application.Services.AppUser.Login;
using Caudal.Application.Services.AppUser.Logout;
using Caudal.Application.Services.AppUser.ValidateSession;
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
        service.AddScoped<AccountConfirmationSender>();
        service.AddScoped<IUserConfirmationUseCase, UserConfirmationUseCase>();
        service.AddScoped<IUserCreateAccountUseCase, UserCreateAccountUseCase>();
        service.AddScoped<IUserLoginUseCase, UserLoginUseCase>();
        service.AddScoped<IUserSessionValidationUseCase, UserSessionValidationUseCase>();
        service.AddScoped<IUserLogoutUseCase, UserLogoutUseCase>();
        return service;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<UserRequest>, UserRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        return services;
    }
}
