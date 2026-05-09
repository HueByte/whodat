# UsersEndpoints

> **File:** `src/api/Whodat.Api/Endpoints/UsersEndpoints.cs`  
> **Kind:** class

Provides HTTP endpoints for user management, including registration, lookup, and the current-user profile operations. It wires routes to handler methods and enforces authorization where needed.

## Remarks

Public lookup resolves a user by handle in two passes: first by the normalized UserName, then by a stored alias. Hidden users are never surfaced via public routes; use `/u/me` to view your own profile when authenticated. The lookup eagerly includes Aliases to ensure aliases are available in the result.

Input constraints are enforced via static constants:
- MaxText (280) limits the user-provided text field.
- MaxAscii (64 KiB) limits the avatar ASCII payload.
- MaxAliases (5) constrains the number of user aliases (used by related logic outside the shown excerpt).

Registration validates the provided handle (via Handles.Normalize), requires a password, and enforces length constraints. A token is generated for the new user, its hash is stored, and the plain token is returned to the caller as part of the TokenResponse. If creation fails due to a duplicate username, a 409 Conflict is returned with an explanatory error; otherwise a 400 Bad Request with detailed error messages is returned.

The Me endpoint requires authentication; it reloads the current user with navigation properties (Aliases) and returns a representation suitable for clients (EntryDto). If the user cannot be determined or the user record is missing, the response is 401 Unauthorized.

The endpoints are mapped as follows:
- GET /u/{handle} → Lookup
- POST /register → Register
- GET /u/me → Me (requires authorization)
- PUT /u/me → Update (requires authorization)
- DELETE /u/me → Delete (requires authorization)

Dependencies used by these handlers include JsonSerializer for metadata serialization, Results/IResult for HTTP results, WhodatUser as the user entity, TokenResponse for login tokens, Handles for handle normalization, EntryDto for response payloads, and Tokens for token generation/ hashing.

## Example

```csharp
// Example: register a new user
using var client = new HttpClient { BaseAddress = new Uri("https://example/api/") };

var register = new
{
    handle = "alice",
    password = "Secr3t!",
    text = "Hello, I'm Alice",
    avatarAscii = "(ASCII ART)",
    metadata = new { roles = new[] { "user" } }
};

var registerResp = await client.PostAsJsonAsync("/register", register);
registerResp.EnsureSuccessStatusCode();
var tokenPayload = await registerResp.Content.ReadFromJsonAsync<TokenResponse>();

// Example: use the token to access the authenticated endpoint
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenPayload.Token);
var me = await client.GetFromJsonAsync<EntryDto>("/u/me");

// Example: lookup a user by handle
var lookup = await client.GetFromJsonAsync<EntryDto>("/u/alice");
```

## Notes

- Aliases are supported and are loaded when retrieving user data (eagerly via Include in queries).
- Public lookups will not disclose hidden users; hidden accounts are effectively invisible unless accessed via /u/me with proper authorization.
- Registration returns a plain token to the client while storing a salted/hashed version server-side.
- Validation errors surface as 400 Bad Request with an error description, while attempting to register a duplicate handle results in 409 Conflict.
- Endpoints are implemented as a stateless static class; request handling is thread-safe under typical ASP.NET Core guidelines.