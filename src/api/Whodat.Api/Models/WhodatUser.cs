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

    /// When true, public lookups (GET /api/u/{handle}) return 404. The owner
    /// can still see their own profile via /api/u/me.
    public bool IsHidden { get; set; }

    /// When true, this user can appear in `whodat random` style discovery.
    /// Hidden trumps this — IsHidden=true is excluded from random regardless.
    /// Defaults to true so new registrations are discoverable by default.
    public bool RandomVisible { get; set; } = true;

    public long RegisteredAt { get; set; }
    public long UpdatedAt { get; set; }

    /// Secondary names that resolve to this user. Capped at 5 in code.
    public List<UserAlias> Aliases { get; set; } = new();
}

/// Secondary handle that maps to a `WhodatUser`. Aliases are normalized
/// (lowercase, [a-z0-9-]) and globally unique across both UserName and Alias.
public class UserAlias
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public WhodatUser? User { get; set; }
    public string Alias { get; set; } = "";
}
