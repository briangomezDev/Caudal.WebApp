namespace Caudal.Domain.Security.Service;

public sealed record AccountRecipient(string FullName, string Email);

public interface IAccountNotifier
{
    Task SendConfirmationAsync(AccountRecipient recipient, string confirmationLink, CancellationToken cancellationToken);
}
