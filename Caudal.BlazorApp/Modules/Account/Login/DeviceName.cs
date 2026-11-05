namespace Caudal.BlazorApp.Modules.Account.Login;

// Convierte el User-Agent en un nombre legible, como "Firefox en Linux", para app_user_session.
public static class DeviceName
{
    private const string Unknown = "Dispositivo desconocido";

    public static string From(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return Unknown;

        var browser = BrowserOf(userAgent);
        var system = SystemOf(userAgent);

        return (browser, system) switch
        {
            (null, null) => Unknown,
            (null, _) => system!,
            (_, null) => browser,
            _ => $"{browser} en {system}"
        };
    }

    // El orden importa: Edge y Opera también dicen "Chrome", y Chrome también dice "Safari".
    private static string? BrowserOf(string userAgent) => userAgent switch
    {
        _ when userAgent.Contains("Edg/") => "Edge",
        _ when userAgent.Contains("OPR/") => "Opera",
        _ when userAgent.Contains("Firefox/") => "Firefox",
        _ when userAgent.Contains("Chrome/") || userAgent.Contains("CriOS/") => "Chrome",
        _ when userAgent.Contains("Safari/") => "Safari",
        _ => null
    };

    // El orden importa: Android también dice "Linux", e iOS también dice "Mac OS X".
    private static string? SystemOf(string userAgent) => userAgent switch
    {
        _ when userAgent.Contains("Windows") => "Windows",
        _ when userAgent.Contains("Android") => "Android",
        _ when userAgent.Contains("iPhone") || userAgent.Contains("iPad") => "iOS",
        _ when userAgent.Contains("Mac OS X") => "macOS",
        _ when userAgent.Contains("Linux") => "Linux",
        _ => null
    };
}
