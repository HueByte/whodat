# WhodatDb

> **File:** `src/api/Whodat.Api/Data/WhodatDb.cs`  
> **Kind:** class

WhodatDb is an Entity Framework Core DbContext used by the API to manage identity-related data and user aliases. It derives from IdentityDbContext&lt;WhodatUser&gt; and exposes a DbSet&lt;UserAlias&gt; to map user aliases to their owners.

## Remarks

- Token lookups occur on every authenticated request, so an index on WhodatUser.TokenHash is configured to keep lookups fast.
- Aliases are globally unique. The Alias property is indexed uniquely and limited to 32 characters to ensure deterministic lookups and compact storage.
- A one-to-many relationship is defined between WhodatUser and UserAlias: a user can have multiple aliases, with UserId serving as the foreign key. Deleting a user cascades to their aliases (OnDelete(DeleteBehavior.Cascade)).
- The OnModelCreating method extends the base Identity model configuration and applies the additional constraints and indexes described above.

## Example

```csharp
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<WhodatDb>()
    .UseInMemoryDatabase("WhodatTest")
    .Options;

using var db = new WhodatDb(options);

// Create a user (assumes WhodatUser inherits from IdentityUser and has an Id field)
var user = new WhodatUser { Id = "u1" };
db.Add(user);
db.SaveChanges();

// Add an alias for the user
var alias = new UserAlias { Alias = "alice", UserId = user.Id };
db.UserAliases.Add(alias);
db.SaveChanges();

// Lookup an alias
var found = db.UserAliases.FirstOrDefault(a => a.Alias == "alice");
```

## Notes

- DbContext instances are not thread-safe; use a separate context per request/operation.
- The Alias property on UserAlias is constrained to 32 characters and must be unique across all users.
- Deleting a WhodatUser cascades to their related UserAlias entries, ensuring referential integrity.
- The TokenHash index on WhodatUser is intended to speed up token-based lookups during authentication; it is not declared as unique. If token management evolves, consider aligning the index with the latest authentication strategy.