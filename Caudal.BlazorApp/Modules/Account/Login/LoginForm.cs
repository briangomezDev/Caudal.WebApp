namespace Caudal.BlazorApp.Modules.Account.Login;

public sealed class LoginForm
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}
