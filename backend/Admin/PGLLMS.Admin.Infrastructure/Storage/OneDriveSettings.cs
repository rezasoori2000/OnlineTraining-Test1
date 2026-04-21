namespace PGLLMS.Admin.Infrastructure.Storage;

public class OneDriveSettings
{
    public const string SectionName = "OneDrive";

    public string TenantId { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;

    /// <summary>UPN (email) of the OneDrive owner, e.g. user@company.com</summary>
    public string UserEmail { get; set; } = default!;

    /// <summary>
    /// Root folder path inside the user's OneDrive where all files are stored.
    /// Use empty string for the drive root. E.g. "PGLLMS" to put everything under /PGLLMS/.
    /// </summary>
    public string RootFolder { get; set; } = "";
}
