using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Caudal.Infraestructure.Mailing;

internal sealed class MailKitEmailTransport : IEmailTransport
{
    private const int TimeoutMilliseconds = 10_000;

    private readonly EmailOptions _options;

    public MailKitEmailTransport(IOptions<EmailOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient { Timeout = TimeoutMilliseconds };

        await client.ConnectAsync(_options.Host, _options.Port, _options.Security, cancellationToken);

        if (!string.IsNullOrEmpty(_options.UserName))
            await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);

        await client.SendAsync(ToMimeMessage(message), cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private MimeMessage ToMimeMessage(EmailMessage message)
    {
        var mime = new MimeMessage { Subject = message.Subject };
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Body = new BodyBuilder { HtmlBody = message.Html, TextBody = message.Text }.ToMessageBody();

        return mime;
    }
}
