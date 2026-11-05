namespace Caudal.BlazorApp.Modules.Account.Register;

public enum RegisterField
{
    FullName,
    Email,
    Password,
    Terms
}

public sealed class RegisterForm
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool Terms { get; set; }
}
