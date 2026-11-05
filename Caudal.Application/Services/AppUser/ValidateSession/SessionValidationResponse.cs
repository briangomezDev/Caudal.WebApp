namespace Caudal.Application.Services.AppUser.ValidateSession;

public record SessionValidationResponse(
DateTime ExpiresAt,
bool Renewed);
