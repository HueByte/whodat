using Microsoft.AspNetCore.Identity;

namespace Whodat.Api.Models;

/// User row. Identity provides the auth-shaped fields (Id, UserName which is
/// our handle, PasswordHash, SecurityStamp, etc.); we add the registry-shaped
/// fields directly. External login providers (e.g. GitHub) live in the
/// `AspNetUserLogins` table — never on this row.
public class WhodatUser : IdentityUser
{
    public string? Text { get; set; }
    public string? AvatarAscii { get; set; }
    public string? MetadataJson { get; set; }

    /// SHA256 of the bearer token issued at the most recent register/login.
    /// Re-issued on every login, so a fresh login revokes older sessions.
    public string TokenHash { get; set; } = "";

    public long RegisteredAt { get; set; }
    public long UpdatedAt { get; set; }
}
