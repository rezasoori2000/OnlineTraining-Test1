using Ganss.Xss;  // namespace remains Ganss.Xss even though package ID is HtmlSanitizer
using AppInterfaces = PGLLMS.Admin.Application.Interfaces;

namespace PGLLMS.Admin.Infrastructure.Security;

public class GanssHtmlSanitizer : AppInterfaces.IHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public GanssHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        // Allow common formatting tags used in lesson content
        _sanitizer.AllowedTags.UnionWith(new[]
        {
            "p", "br", "strong", "em", "u", "s", "ul", "ol", "li",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "blockquote", "code", "pre", "hr",
            "table", "thead", "tbody", "tr", "th", "td",
            "img", "a", "span", "div", "figure", "figcaption"
        });

        _sanitizer.AllowedAttributes.UnionWith(new[]
        {
            "href", "src", "alt", "title", "class", "id",
            "width", "height", "target", "rel", "style"
        });

        // Allow only safe CSS properties
        _sanitizer.AllowedCssProperties.UnionWith(new[]
        {
            "color", "background-color", "font-weight", "font-style",
            "text-align", "text-decoration", "font-size", "margin",
            "padding", "border"
        });

        // Allow http/https/mailto for links; data: required for base64 PDF images
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.AllowedSchemes.Add("data");
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _sanitizer.Sanitize(html);
    }
}
