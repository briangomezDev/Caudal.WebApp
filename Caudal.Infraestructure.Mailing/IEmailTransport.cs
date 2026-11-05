namespace Caudal.Infraestructure.Mailing;

internal sealed record EmailMessage(string ToAddress, string ToName, string Subject, string Html, string Text);

// Cómo sale el correo. Cambiar de SMTP a la API de un proveedor solo requiere otra implementación.
internal interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
