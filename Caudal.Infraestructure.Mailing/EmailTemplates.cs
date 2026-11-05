using System.Net;
using System.Reflection;

namespace Caudal.Infraestructure.Mailing;

// Plantillas embebidas en Templates/. Cada correo es <nombre>.html, dentro de Layout.html, y <nombre>.txt.
internal sealed class EmailTemplates
{
    private const string LayoutName = "Layout.html";
    private const string ContentPlaceholder = "{{content}}";

    public (string Html, string Text) Render(string name, IReadOnlyDictionary<string, string> values)
    {
        // Solo el HTML se codifica: un nombre como "<b>Ana" debe verse tal cual, no interpretarse.
        var content = Fill(Read($"{name}.html"), values, WebUtility.HtmlEncode);
        var html = Read(LayoutName).Replace(ContentPlaceholder, content);
        var text = Fill(Read($"{name}.txt"), values, value => value);

        return (html, text);
    }

    private static string Fill(string template, IReadOnlyDictionary<string, string> values, Func<string, string> encode)
    {
        return values.Aggregate(template, (result, value) => result.Replace($"{{{{{value.Key}}}}}", encode(value.Value)));
    }

    private static string Read(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = $"{typeof(EmailTemplates).Namespace}.Templates.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The email template '{fileName}' does not exist.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
