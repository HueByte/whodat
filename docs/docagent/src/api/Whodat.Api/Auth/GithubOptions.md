# GithubOptions

> **File:** `src/api/Whodat.Api/Auth/GithubOptions.cs`  
> **Kind:** class

GithubOptions encapsulates the optional configuration for GitHub authentication by exposing the OAuth client identifier. It exists to centralize the presence of a GitHub OAuth client ID and to provide a simple readiness check via IsConfigured.

## Remarks

IsConfigured evaluates whether a usable ClientId has been provided. It returns true when ClientId is neither null nor whitespace. Because ClientId is nullable, omitting it allows the application to disable GitHub login paths. No validation is performed on the value beyond presence; you should supply a valid client ID from GitHub when enabling authentication.

## Example

```csharp
// Common usage example
var options = new GithubOptions { ClientId = "your-client-id" };

if (options.IsConfigured)
{
    // Enable GitHub authentication in the login flow
}
```

## Notes

- ClientId is nullable; IsConfigured treats null or whitespace as not configured.
- This class does not manage secrets or redirect URIs; use separate configuration for those.
- IsConfigured is a lightweight readiness check and not a security feature.
- No thread-safety guarantees beyond standard .NET memory model; best to configure once at startup.