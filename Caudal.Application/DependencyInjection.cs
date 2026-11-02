using Caudal.Application.Services.AppUser.CreateAccount;
using Caudal.Infraestructure;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caudal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddInfrastructure(configuration);
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
