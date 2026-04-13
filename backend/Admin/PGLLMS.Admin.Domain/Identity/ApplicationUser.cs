using Microsoft.AspNetCore.Identity;

namespace PGLLMS.Admin.Domain.Identity;

/// <summary>
/// Supports two identity providers:
///  1. Azure Active Directory (ExternalProvider = "AzureAD", ExternalId = Azure OID)
///  2. Local email/password (ExternalProvider = null)
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    /// <summary>
    /// Identifies the external identity provider (e.g. "AzureAD").
    /// Null for local email/password accounts.
    /// </summary>
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// Object ID from the external provider.
    /// Null for local email/password accounts.
    /// </summary>
    public string? ExternalId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
