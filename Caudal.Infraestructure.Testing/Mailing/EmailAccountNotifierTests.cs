using Caudal.Domain.Security.Service;
using Caudal.Infraestructure.Mailing;
using Moq;
using Xunit;

namespace Caudal.Infraestructure.Testing.Mailing;

public class EmailAccountNotifierTests
{
    private const string Link = "https://caudal.test/account/confirm?token=abc&x=1";

    private readonly Mock<IEmailTransport> _transport = new();

    private async Task<EmailMessage> SendAsync(string fullName = "Brian Gomez")
    {
        EmailMessage? sent = null;
        _transport
            .Setup(t => t.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sent = message)
            .Returns(Task.CompletedTask);

        var notifier = new EmailAccountNotifier(new EmailTemplates(), _transport.Object);
        await notifier.SendConfirmationAsync(new AccountRecipient(fullName, "brian@caudal.com"), Link, CancellationToken.None);

        return sent!;
    }

    // Bueno: el mensaje llega completo y en sus dos formatos.

    [Fact]
    public async Task SendConfirmationAsync_BuildsMessageForRecipient()
    {
        // Act
        var message = await SendAsync();

        // Assert
        Assert.Equal("brian@caudal.com", message.ToAddress);
        Assert.Equal("Brian Gomez", message.ToName);
        Assert.Equal("Confirma tu correo en Caudal", message.Subject);
    }

    [Fact]
    public async Task SendConfirmationAsync_RendersLayoutAndValues()
    {
        // Act
        var message = await SendAsync();

        // Assert
        Assert.Contains("<!DOCTYPE html>", message.Html);
        Assert.Contains("Hola, Brian Gomez", message.Html);
        Assert.Contains("href=\"https://caudal.test/account/confirm?token=abc&amp;x=1\"", message.Html);
        Assert.Contains(Link, message.Text);
        Assert.DoesNotContain("{{", message.Html);
        Assert.DoesNotContain("{{", message.Text);
    }

    // Malo: lo que escribe el usuario nunca se interpreta como HTML.

    [Fact]
    public async Task SendConfirmationAsync_WithHtmlInName_EncodesItOnlyInHtml()
    {
        // Act
        var message = await SendAsync("<img src=x>");

        // Assert
        Assert.DoesNotContain("<img src=x>", message.Html);
        Assert.Contains("&lt;img src=x&gt;", message.Html);
        Assert.Contains("<img src=x>", message.Text);
    }
}
