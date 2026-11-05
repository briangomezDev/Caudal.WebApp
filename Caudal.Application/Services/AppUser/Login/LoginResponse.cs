namespace Caudal.Application.Services.AppUser.Login;

public record LoginResponse(
Guid UserId,
Guid SessionId,
string FullName,
DateTime ExpiresAt);
