using System.ComponentModel.DataAnnotations;

namespace PGLLMS.Admin.API.Auth;

public class LocalLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
