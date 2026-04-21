using Microsoft.AspNetCore.Mvc;

namespace PGLLMS.Admin.API.Controllers;

/// <summary>
/// Handles server-side PDF text extraction via the Marker OCR sidecar.
/// Called by the frontend when PDF.js text layer extraction returns insufficient text
/// (i.e. the PDF is image-based / scanned).
/// </summary>
[ApiController]
[Route("api/admin/pdf")]
public class PdfController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PdfController> _logger;

    public PdfController(IHttpClientFactory httpClientFactory, ILogger<PdfController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Accepts a PDF file and returns the extracted plain text using the Marker OCR service.
    /// Returns { "text": "..." } on success.
    /// Returns 502 if the Marker sidecar is unreachable, so the frontend can gracefully fall back.
    /// </summary>
    [HttpPost("ocr")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    [ProducesResponseType(typeof(PdfOcrResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ExtractText(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            return BadRequest(new { message = "Only PDF files are supported." });

        try
        {
            var marker = _httpClientFactory.CreateClient("Marker");

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "pdf_file", file.FileName);

            // Marker API: POST /marker with form field "pdf_file"
            // Response: { "markdown": "...", "metadata": {...} }
            var response = await marker.PostAsync("/marker", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Marker OCR returned {Status} for file {File}",
                    response.StatusCode, file.FileName);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "OCR service returned an error." });
            }

            var json = await response.Content.ReadFromJsonAsync<MarkerResponse>(
                cancellationToken: ct);

            var text = json?.Markdown ?? "";

            // Strip markdown syntax (headers, bold, bullets) — we want plain prose for embedding
            text = StripMarkdown(text);

            return Ok(new PdfOcrResponse(text));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Marker OCR sidecar unreachable");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "OCR service is not available." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "OCR request timed out." });
        }
    }

    // Remove common Markdown syntax to get clean plain text for the embedding pipeline
    private static string StripMarkdown(string md)
    {
        if (string.IsNullOrWhiteSpace(md)) return "";
        // Headers: ## Title → Title
        md = System.Text.RegularExpressions.Regex.Replace(md, @"^#{1,6}\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Bold/italic: **text** or *text* → text
        md = System.Text.RegularExpressions.Regex.Replace(md, @"\*{1,3}(.+?)\*{1,3}", "$1");
        // Inline code: `code` → code
        md = System.Text.RegularExpressions.Regex.Replace(md, @"`([^`]+)`", "$1");
        // Code blocks
        md = System.Text.RegularExpressions.Regex.Replace(md, @"```[\s\S]*?```", " ");
        // List bullets
        md = System.Text.RegularExpressions.Regex.Replace(md, @"^\s*[-*+]\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Collapse whitespace
        md = System.Text.RegularExpressions.Regex.Replace(md, @"\n{3,}", "\n\n");
        return md.Trim();
    }

    private sealed class MarkerResponse
    {
        public string? Markdown { get; set; }
    }

    public sealed record PdfOcrResponse(string Text);
}
