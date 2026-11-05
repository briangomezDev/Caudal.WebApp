using Caudal.Application.Services.AppUser.CreateAccount;
using ErrorOr;
using FluentValidation;

namespace Caudal.BlazorApp.Modules.Account.Register;

/// <summary>
/// Única puerta de la feature hacia Application: traduce el formulario a UserRequest
/// y los códigos SECURITY-* a mensajes en español por campo.
/// </summary>
public sealed class RegisterFacade(IServiceScopeFactory scopes, IValidator<UserRequest> validator)
{
    private const string ReviewFieldsMessage = "Revisa los campos marcados";
    private const string TermsMessage = "Debes aceptar los términos y la política de privacidad";

    // Códigos de UserRequestValidator / UserCreateAccountUseCase -> campo del formulario.
    private static readonly Dictionary<string, (RegisterField Field, string Message)> FieldErrorsByCode = new()
    {
        ["SECURITY-0001"] = (RegisterField.FullName, "El nombre es requerido"),
        ["SECURITY-0002"] = (RegisterField.FullName, "El nombre no puede superar 150 caracteres"),
        ["SECURITY-0003"] = (RegisterField.Email, "El correo es requerido"),
        ["SECURITY-0004"] = (RegisterField.Email, "El correo no puede superar 100 caracteres"),
        ["SECURITY-0005"] = (RegisterField.Email, "El correo no es válido"),
        ["SECURITY-0012"] = (RegisterField.Email, "Ya existe una cuenta con este correo"),
        ["SECURITY-0006"] = (RegisterField.Password, "La contraseña es requerida"),
        ["SECURITY-0007"] = (RegisterField.Password, "La contraseña debe tener al menos 8 caracteres"),
        ["SECURITY-0009"] = (RegisterField.Password, "La contraseña no puede superar 128 caracteres"),
        ["SECURITY-0013"] = (RegisterField.Password, "La contraseña debe incluir un número"),
        ["SECURITY-0014"] = (RegisterField.Password, "La contraseña debe incluir una mayúscula"),
    };

    private static readonly Dictionary<string, string> GeneralMessagesByCode = new()
    {
        ["SECURITY-0011"] = "Ocurrió un error inesperado. Intenta de nuevo.",
    };

    public IReadOnlyDictionary<RegisterField, string> Validate(RegisterForm form)
    {
        var fieldErrors = new Dictionary<RegisterField, string>();

        foreach (var failure in validator.Validate(ToRequest(form)).Errors)
        {
            if (FieldErrorsByCode.TryGetValue(failure.ErrorCode, out var error))
                fieldErrors.TryAdd(error.Field, error.Message);
        }

        // Solo existe en la interfaz: no viaja en la petición.
        if (!form.Terms)
            fieldErrors[RegisterField.Terms] = TermsMessage;

        return fieldErrors;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterForm form, CancellationToken cancellationToken)
    {
        var fieldErrors = Validate(form);
        if (fieldErrors.Count > 0)
            return new RegisterResult(ReviewFieldsMessage, fieldErrors);

        // Un scope por operación: el del circuito vive tanto como la conexión del usuario,
        // y un DbContext que falló conservaría sus entidades para el siguiente intento.
        await using var scope = scopes.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUserCreateAccountUseCase>();

        var result = await useCase.CreateAccountAsync(ToRequest(form), cancellationToken);

        return result.IsError ? ToResult(result.Errors) : RegisterResult.Success;
    }

    private static UserRequest ToRequest(RegisterForm form) =>
        new(form.FullName.Trim(), form.Email.Trim(), form.Password);

    private static RegisterResult ToResult(List<Error> errors)
    {
        var fieldErrors = new Dictionary<RegisterField, string>();
        string? general = null;

        foreach (var error in errors)
        {
            if (FieldErrorsByCode.TryGetValue(error.Code, out var fieldError))
                fieldErrors.TryAdd(fieldError.Field, fieldError.Message);
            else
                general ??= GeneralMessagesByCode.GetValueOrDefault(error.Code, error.Description);
        }

        return new RegisterResult(general ?? ReviewFieldsMessage, fieldErrors);
    }
}
