namespace PGLLMS.Admin.Application.Interfaces;

public interface IOneDriveService
{
    /// <summary>
    /// Uploads a stream to the given remote path in OneDrive, creating intermediate
    /// folders automatically. Overwrites an existing file at the same path.
    /// Returns the remote path as stored (same as <paramref name="remotePath"/>).
    /// </summary>
    Task<string> UploadFileAsync(string remotePath, Stream content, string contentType = "application/pdf", CancellationToken ct = default);

    /// <summary>
    /// Returns a short-lived pre-authenticated download URL for the file at <paramref name="remotePath"/>.
    /// The URL is valid for approximately 1 hour and can be used directly by a browser or PDF.js.
    /// </summary>
    Task<string?> GetDownloadUrlAsync(string remotePath, CancellationToken ct = default);
}
