namespace PGLLMS.Admin.Infrastructure.Storage;

public class LocalFileServerSettings
{
    public const string SectionName = "LocalFileServer";

    /// <summary>
    /// Absolute path to the root folder where PDF files are stored.
    /// Can be a local path or a mapped network drive, e.g. "I:\IT\Documents\PGL-LMS".
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the Portal API used to build download links returned to the browser,
    /// e.g. "http://localhost:5001". Files are served at {PortalFileBaseUrl}/api/files/{remotePath}.
    /// </summary>
    public string PortalFileBaseUrl { get; set; } = "http://localhost:5001";
}
