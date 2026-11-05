namespace Caudal.Application.Services.AppUser.Confirmation;

// Sale de la configuración, nunca de la petición: la cabecera Host la puede falsificar un atacante.
public sealed class AccountLinkOptions
{
    public string BaseUrl { get; set; } = "";
}
