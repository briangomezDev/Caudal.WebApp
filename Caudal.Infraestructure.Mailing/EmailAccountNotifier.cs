using Caudal.Domain.Security.Service;

namespace Caudal.Infraestructure.Mailing;

internal sealed class EmailAccountNotifier : IAccountNotifier
{
    private readonly EmailTemplates _templates;
    private readonly IEmailTransport _transport;

    public EmailAccountNotifier(EmailTemplates templates, IEmailTransport transport)
    {
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public Task SendConfirmationAsync(AccountRecipient recipient, string confirmationLink, CancellationToken cancellationToken)
    {
        var (html, text) = _templates.Render("AccountConfirmation", new Dictionary<string, string>
        {
            ["name"] = recipient.FullName,
            ["link"] = confirmationLink
        });

        return _transport.SendAsync(
            new EmailMessage(recipient.Email, recipient.FullName, "Confirma tu correo en Caudal", html, text),
            cancellationToken);
    }
}
