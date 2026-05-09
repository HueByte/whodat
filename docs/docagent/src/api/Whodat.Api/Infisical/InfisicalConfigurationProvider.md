# InfisicalConfigurationProvider

> **File:** `src/api/Whodat.Api/Infisical/InfisicalConfigurationProvider.cs`  
> **Kind:** class

```csharp
/// Pulls secrets from an Infisical project and exposes them as configuration
/// keys. Naming convention: secret keys use `__` as the section separator, the
/// same way ASP.NET Core treats environment variables. So a secret named
/// `GitHub__ClientId` in Infisical becomes `Configuration["GitHub:ClientId"]`.
public class InfisicalConfigurationProvider(InfisicalConfigurationSource source) : ConfigurationProvider
```


InfisicalConfigurationProvider pulls secrets from an Infisical project and exposes them as ASP.NET Core configuration keys. It maps secret keys using the Infisical "__" separator to ASP.NET Core’s colon-delimited sections (e.g., GitHub__ClientId becomes GitHub:ClientId). When enabled, it authenticates with Infisical using the configured ClientId/ClientSecret, lists secrets for the given project/environment/path, and loads them into the configuration data.

## Remarks
- Only runs when InfisicalConfigurationSource.Options.Enabled is true; otherwise it does nothing.
- Validates that ProjectId, ClientId, and ClientSecret are provided when enabled; otherwise throws InvalidOperationException.
- Uses Infisical SDK to authenticate, fetch secrets (respecting EnvironmentSlug, SecretPath, and Recursive options), and populate configuration keys in a case-insensitive dictionary.