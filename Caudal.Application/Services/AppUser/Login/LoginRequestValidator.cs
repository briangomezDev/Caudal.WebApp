using FluentValidation;

namespace Caudal.Application.Services.AppUser.Login;

// Solo exige que vengan los datos: las reglas de contraseña aplican al crearla, no al usarla.
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("SECURITY-0003").WithMessage("The email is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("SECURITY-0006").WithMessage("The password is required");
    }
}
