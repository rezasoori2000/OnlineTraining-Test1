using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PGLLMS.Admin.Infrastructure.Storage;

namespace PGLLMS.Portal.API.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly LocalFileServerSettings _settings;
    private readonly ILogger<FilesController> _logger;

    private static readonly Dictionary<string, string> _mimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf",  "application/pdf" },
        { ".png",  "image/png" },
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif",  "image/gif" },
        { ".webp", "image/webp" },
    };

    public FilesController(
        IOptions<LocalFileServerSettings> settings,
        ILogger<FilesController> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Streams a file stored on disk at {StoragePath}/{*path}.
    /// Used by the portal frontend to display PDFs and other content.
    /// </summary>
    [HttpGet("{*path}")]
    public IActionResult GetFile(string path)
    {
        if (string.IsNullOrWhiteSpace(_settings.StoragePath))
            return StatusCode(503, "File storage is not configured.");

        // Prevent path traversal
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(
                Path.Combine(_settings.StoragePath, path.Replace('/', Path.DirectorySeparatorChar)));

            if (!fullPath.StartsWith(Path.GetFullPath(_settings.StoragePath), StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid path.");
        }
        catch
        {
            return BadRequest("Invalid path.");
        }

        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {Path}", fullPath);
            return NotFound();
        }

        var ext = Path.GetExtension(fullPath);
        var contentType = _mimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, contentType, enableRangeProcessing: true);
    }
}
