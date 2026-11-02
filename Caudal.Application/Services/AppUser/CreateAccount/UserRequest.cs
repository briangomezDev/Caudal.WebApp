namespace Caudal.Application.Services.AppUser.CreateAccount;

public record UserRequest(
string FullName, 
string Email, 
string Password);
