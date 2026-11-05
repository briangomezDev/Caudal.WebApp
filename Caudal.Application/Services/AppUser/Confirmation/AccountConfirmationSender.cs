using Caudal.Domain.Security.Entity;
using Caudal.Domain.Security.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Caudal.Application.Services.AppUser.Confirmation;

internal sealed class AccountConfirmationSender
{
    private readonly ILogger<AccountConfirmationSender> _logger;
    private readonly IAccountConfirmationTokens _tokens;
    private readonly IAccountNotifier _notifier;
    private readonly AccountLinkOptions _links;

    public AccountConfirmationSender(ILogger<AccountConfirmationSender> logger,
                                     IAccountConfirmationTokens tokens,
                                     IAccountNotifier notifier,
                                     IOptions<AccountLinkOptions> links)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        ArgumentNullException.ThrowIfNull(links);
        _links = links.Value;
    }

    /// <summary>
    /// No lanza si falla el envío: la cuenta ya existe y el usuario puede pedir otro enlace.
    /// </summary>
    public async Task SendAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var token = _tokens.Create(user.UserId!, user.Email!);
            var link = $"{_links.BaseUrl.TrimEnd('/')}/account/confirm?token={Uri.EscapeDataString(token)}";

            await _notifier.SendConfirmationAsync(
                new AccountRecipient(user.FullName ?? string.Empty, user.Email!.Value!),
                link,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "The account confirmation could not be sent");
        }
    }
}
