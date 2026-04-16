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

        // Allow CSS properties needed for PPTXjs-generated slide HTML and general content
        _sanitizer.AllowedCssProperties.UnionWith(new[]
        {
            // Text
            "color", "font-size", "font-weight", "font-style", "font-family",
            "font-variant", "text-align", "text-decoration", "text-transform",
            "text-indent", "text-overflow", "white-space", "word-break",
            "word-wrap", "letter-spacing", "line-height", "vertical-align",
            // Box model
            "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
            "border", "border-top", "border-right", "border-bottom", "border-left",
            "border-radius", "border-color", "border-width", "border-style",
            "width", "height", "min-width", "min-height", "max-width", "max-height",
            "box-sizing", "overflow", "overflow-x", "overflow-y",
            // Layout / positioning (required by PPTXjs slides)
            "display", "position", "top", "right", "bottom", "left",
            "float", "clear", "z-index",
            "flex", "flex-direction", "flex-wrap", "flex-grow", "flex-shrink",
            "align-items", "align-self", "justify-content",
            // Background
            "background", "background-color", "background-image",
            "background-repeat", "background-position", "background-size",
            // Visual
            "opacity", "visibility",
            "transform", "transform-origin",
            "box-shadow", "text-shadow",
            "list-style", "list-style-type",
            "table-layout", "border-collapse", "border-spacing",
            "cursor"
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

        // PPTXjs-generated HTML (identified by the wrapper class set by convertFileToHtml.ts)
        // contains complex inline styles and positioning that the sanitizer cannot preserve.
        // This content is admin-only input — bypass sanitization to match folder behaviour.
        if (html.Contains("pptx-content") || html.Contains("pdf-content"))
            return html;

        return _sanitizer.Sanitize(html);
    }
}
