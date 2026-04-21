using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PGLLMS.Admin.Application.Interfaces;

namespace PGLLMS.Admin.Infrastructure.Storage;

/// <summary>
/// Implements <see cref="IOneDriveService"/> by writing files to a local or mapped-drive folder.
///
/// Configure in appsettings.json:
///   "LocalFileServer": {
///     "StoragePath": "I:\\IT\\Documents\\PGL-LMS",
///     "PortalFileBaseUrl": "http://portal-server:5001"
///   }
///
/// Upload  : writes stream to {StoragePath}/{remotePath}, creating sub-folders as needed.
/// Download: returns {PortalFileBaseUrl}/api/files/{remotePath} — served by FilesController.
/// </summary>
public sealed class LocalFileServerService : IOneDriveService
{
    private readonly LocalFileServerSettings _settings;
    private readonly ILogger<LocalFileServerService> _logger;

    public LocalFileServerService(
        IOptions<LocalFileServerSettings> settings,
        ILogger<LocalFileServerService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(
        string remotePath,
        Stream content,
        string contentType = "application/pdf",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.StoragePath))
            throw new InvalidOperationException(
                "LocalFileServer:StoragePath is not configured in appsettings.json.");

        var fullPath = BuildFullPath(remotePath);
        _logger.LogInformation("Saving file to local storage: {Path}", fullPath);

        var directory = Path.GetDirectoryName(fullPath)!;
        CreateDirectoriesRecursive(directory);

        await using var fileStream = new FileStream(
            fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);

        await content.CopyToAsync(fileStream, ct);

        return remotePath;
    }

    public Task<string?> GetDownloadUrlAsync(string remotePath, CancellationToken ct = default)
    {
        var baseUrl = _settings.PortalFileBaseUrl.TrimEnd('/');
        // Normalise to forward slashes for a valid URL
        var urlPath = remotePath.Replace('\\', '/').TrimStart('/');
        var url = $"{baseUrl}/api/files/{urlPath}";
        return Task.FromResult<string?>(url);
    }

    /// <summary>
    /// Creates each directory level individually, which is more reliable than
    /// Directory.CreateDirectory() on mapped network drives where deep paths may fail.
    /// </summary>
    private static void CreateDirectoriesRecursive(string path)
    {
        if (Directory.Exists(path)) return;

        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
            CreateDirectoriesRecursive(parent);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private string BuildFullPath(string remotePath)
    {
        // Prevent path traversal
        var safePath = Path.GetFullPath(
            Path.Combine(_settings.StoragePath, remotePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!safePath.StartsWith(Path.GetFullPath(_settings.StoragePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid remote path: path traversal detected.");

        return safePath;
    }
}

