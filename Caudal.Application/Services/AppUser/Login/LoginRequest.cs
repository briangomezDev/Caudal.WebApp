namespace Caudal.Application.Services.AppUser.Login;

public record LoginRequest(
string Email,
string Password,
string Device,
bool RememberMe);
