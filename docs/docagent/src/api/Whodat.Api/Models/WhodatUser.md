# WhodatUser.cs

> **Source:** `src/api/Whodat.Api/Models/WhodatUser.cs`

## Contents

- [UserAlias](#useralias)
- [WhodatUser](#whodatuser)

---

<a id="useralias"></a>

## UserAlias

> **File:** `src/api/Whodat.Api/Models/WhodatUser.cs`  
> **Kind:** class

Represents a secondary alias that maps to a WhodatUser. Aliases are normalized to lowercase and restricted to the characters a-z, 0-9, and dash, and are globally unique across both UserName and Alias.

## Remarks

This is a lightweight data model used to associate alternate handles with a WhodatUser. It contains an Id (primary key), a UserId foreign key, an optional User navigation property, and the Alias value. The alias normalization and the global uniqueness constraint are intended to be enforced by the surrounding application or database layer, rather than within this class itself. The User navigation property is nullable, indicating that an alias may exist without a loaded related user in memory; the relationship direction and constraints depend on the ORM configuration.

## Example

```csharp
// Example: associating an alias with an existing user
var user = new WhodatUser { Id = "user-123", UserName = "alice" };
var alias = new UserAlias
{
    User = user,
    Alias = "alice-dev" // typically normalized to lowercase at persistence time
};
```

## Notes
- This class is a plain data container (POCO/DTO) used with an ORM; no thread-safety guarantees are provided.
- Normalization and global uniqueness are expected to be enforced by the data layer (e.g., before persisting) rather than by this class.
- The User navigation property is optional; ensure proper linking when the related WhodatUser is known.
- Consider indexing Alias (and possibly UserId) in the database to optimize lookups and enforce uniqueness.

---

<a id="whodatuser"></a>

## WhodatUser

> **File:** `src/api/Whodat.Api/Models/WhodatUser.cs`  
> **Kind:** class

WhodatUser is the application’s user model that extends IdentityUser with registry-specific fields to support profile data, privacy controls, and discoverability, while IdentityUser provides the authentication-related properties (Id, UserName, PasswordHash, SecurityStamp, etc.).

## Remarks

- TokenHash stores the SHA256 hash of the bearer token issued at the most recent register/login. It is re-issued on every login, so a fresh login revokes older sessions.
- IsHidden, when true, causes public lookups (GET /api/u/{handle}) to return 404. The owner can still view their own profile via /api/u/me.
- RandomVisible controls whether the user can appear in random/discovery flows. If IsHidden is true, the user is excluded regardless. Defaults to true so new registrations are discoverable by default.
- Aliases represents secondary names that resolve to this user. The code caps this collection at 5 items. Aliases are stored in a separate UserAlias model.
- RegisteredAt and UpdatedAt are long integers representing timestamps for creation and last modification. These are typically populated by the application layer.
- This class inherits IdentityUser, so core authentication fields (like Id, UserName, PasswordHash, SecurityStamp) come from the base type, and external login providers live in the AspNetUserLogins table rather than on this row.
- Text, AvatarAscii, and MetadataJson are optional profile fields intended to enrich the user profile without impacting authentication.

## Example

```csharp
using System;
using System.Collections.Generic;

var user = new WhodatUser
{
    UserName = "alice",
    Text = "New member on Whodat",
    AvatarAscii = "(^_^)",
    RandomVisible = true,
    IsHidden = false,
    RegisteredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    Aliases = new List<UserAlias>()
};
```

## Notes

- The IsHidden flag affects public visibility but does not prevent the user from authenticating or accessing private endpoints when properly authorized.
- TokenHash implies token-based session revocation behavior; ensure secure handling of token issuance and storage.
- Aliases are capped at 5; attempting to add more may require application-side validation.
- External login providers are not stored directly on this entity; integration with providers relies on AspNetUserLogins and related identity infrastructure.

---