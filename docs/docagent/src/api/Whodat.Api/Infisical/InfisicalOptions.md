# InfisicalOptions

> **File:** `src/api/Whodat.Api/Infisical/InfisicalOptions.cs`  
> **Kind:** class

Holds configuration options for the Infisical integration used by the API client. It exposes toggles, server details, environment scope, and optional credentials needed to retrieve secrets.

## Remarks
Defaults are applied when properties are not explicitly set: SiteUrl defaults to https://app.infisical.com; EnvironmentSlug defaults to dev; SecretPath defaults to /. Recursive defaults to true. ClientId and ClientSecret are optional and used when authenticating via OAuth. ProjectId is optional and may be omitted if the integration relies on other identifiers. When Enabled is false, the Infisical connector should skip initialization. This class is a simple data container and does not perform validation beyond what consumers implement.

## Example
```csharp
var options = new InfisicalOptions
{
    Enabled = true,
    SiteUrl = "https://app.infisical.com",
    ProjectId = "proj-001",
    EnvironmentSlug = "production",
    SecretPath = "/secrets",
    Recursive = true,
    ClientId = "my-client-id",
    ClientSecret = "my-client-secret"
};
```

## Notes
- Nullable properties (ProjectId, ClientId, ClientSecret) may be null and should be handled accordingly.
- Defaults apply automatically for SiteUrl, EnvironmentSlug, SecretPath, and Recursive when not explicitly set.
- This is a plain data holder; it does not perform validation. Validate values at the call site as needed.
- Treat ClientSecret as sensitive; avoid logging or exposing it in UIs or error messages.