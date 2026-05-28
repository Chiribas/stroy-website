using Ganss.Xss;
using Core.Interfaces;

namespace Infrastructure.Services;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private static readonly string[] AllowedIframeHosts =
    {
        "www.youtube.com", "youtube.com",
        "rutube.ru", "vk.com", "vkvideo.ru"
    };

    public string Sanitize(string html) => CreateSanitizer().Sanitize(html ?? string.Empty);

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "br", "strong", "em", "u", "h2", "h3",
            "ul", "ol", "li", "a", "img", "iframe",
            "blockquote", "figure", "figcaption"
        })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in new[]
        {
            "href", "src", "alt", "class", "title",
            "allowfullscreen", "frameborder", "width", "height"
        })
        {
            sanitizer.AllowedAttributes.Add(attr);
        }

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");

        sanitizer.PostProcessNode += (sender, e) =>
        {
            if (e.Node is AngleSharp.Html.Dom.IHtmlInlineFrameElement iframe)
            {
                var src = iframe.GetAttribute("src");
                if (!IsTrustedIframe(src))
                    iframe.Remove();
            }
        };

        return sanitizer;
    }

    private static bool IsTrustedIframe(string? src)
    {
        if (string.IsNullOrWhiteSpace(src)) return false;
        if (!Uri.TryCreate(src, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        return AllowedIframeHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }
}
