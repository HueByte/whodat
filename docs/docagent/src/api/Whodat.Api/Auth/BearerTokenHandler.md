# BearerTokenHandler.cs

> **Source:** `src/api/Whodat.Api/Auth/BearerTokenHandler.cs`

## Contents

- [BearerTokenHandler](#bearertokenhandler)
- [BearerTokenOptions](#bearertokenoptions)

---

<a id="bearertokenhandler"></a>

## BearerTokenHandler

> **File:** `src/api/Whodat.Api/Auth/BearerTokenHandler.cs`  
> **Kind:** class

Resolves an Authorization: Bearer token into a WhodatUser by hashing the token and locating the corresponding record via the TokenHash index. This custom bearer-based authentication exists because Identity's cookies/JWT flows do not fit our CLI-issued long-lived tokens, so authentication is performed manually.

## Remarks
This class extends `AuthenticationHandler<BearerTokenOptions>` and runs as part of ASP.NET Core's authentication pipeline. It reads the Authorization header and requires the Bearer scheme; if the header is missing or malformed, authentication yields NoResult() so other handlers may apply or the request may be treated as anonymous.

The token is extracted from the header, hashed with Tokens.Hash, and the resulting value is used to locate a user via the injected UserManager. If no user matches, authentication fails with the message "invalid token".

On success, a ClaimsIdentity is created containing:
- NameIdentifier: the user's Id
- Name: the user's UserName (or an empty string if null)
This identity is wrapped in an AuthenticationTicket using the Bearer scheme and returned as AuthenticateResult.Success.

This approach is stateless from the client perspective and relies on token hashes rather than storing plaintext tokens.

## Example
```csharp
// Example: client authenticates to a protected endpoint using a bearer token
var token = "REPLACE_WITH_TOKEN";
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var response = await client.GetAsync("https://yourapi/api/whodat/profile");
// handle response (e.g., success, 401, etc.)
```

## Notes
- The handler is invoked per request as part of the authentication middleware.
- It is designed for CLI-issued long-lived tokens and uses a hashed token lookup rather than plaintext storage.
- If UserName is null, the Name claim defaults to an empty string to avoid null claims.
- The lookup uses FirstOrDefaultAsync on the Users set, which should be backed by an indexed TokenHash column for performance.
- On invalid tokens, authentication fails with a generic message to avoid leaking token details. 
- This implementation relies on the Bearer scheme; ensure the authentication pipeline is configured accordingly when registering this handler.

---

<a id="bearertokenoptions"></a>

## BearerTokenOptions

> **File:** `src/api/Whodat.Api/Auth/BearerTokenHandler.cs`  
> **Kind:** class

```csharp
public class BearerTokenOptions : AuthenticationSchemeOptions
```


BearerTokenOptions is a lightweight options class used to configure a bearer token authentication scheme. It derives from AuthenticationSchemeOptions but does not add any additional properties or behavior, serving as a distinct type to identify the Bearer Token handler in ASP.NET Core. Use it when registering or configuring an authentication scheme that relies on Bearer tokens.

## Remarks
- Inherits from AuthenticationSchemeOptions with no extra members.
- Acts as a specific options type for a Bearer token authentication scheme.
- No custom configuration is defined within this class itself; properties come from the base options or the corresponding handler.

---