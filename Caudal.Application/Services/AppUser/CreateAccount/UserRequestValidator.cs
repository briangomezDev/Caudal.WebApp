using FluentValidation;

namespace Caudal.Application.Services.AppUser.CreateAccount;

public class UserRequestValidator : AbstractValidator<UserRequest>
{
    public UserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("SECURITY-0001").WithMessage("The name is required")
            .MaximumLength(150).WithErrorCode("SECURITY-0002").WithMessage("The name must be 150 characters or fewer");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("SECURITY-0003").WithMessage("The email is required")
            .MaximumLength(100).WithErrorCode("SECURITY-0004").WithMessage("The email must be 100 characters or fewer")
            .EmailAddress().WithErrorCode("SECURITY-0005").WithMessage("The email is invalid");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("SECURITY-0006").WithMessage("The password is required")
            .MinimumLength(8).WithErrorCode("SECURITY-0007").WithMessage("The password must be at least 8 characters")
            .MaximumLength(128).WithErrorCode("SECURITY-0009").WithMessage("The password must be 128 characters or fewer");

    }
}
