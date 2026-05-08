namespace Whodat.Api.Infisical;

public class InfisicalOptions
{
    public bool Enabled { get; set; }
    public string SiteUrl { get; set; } = "https://app.infisical.com";
    public string? ProjectId { get; set; }
    public string EnvironmentSlug { get; set; } = "dev";
    public string SecretPath { get; set; } = "/";
    public bool Recursive { get; set; } = true;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// Optional vault-name → config-key map. Lets you keep snake_case secret
    /// names in Infisical (consistent across all projects) while still binding
    /// to idiomatic .NET sectioned config keys like `GitHub:ClientId`.
    /// Example:
    ///   Mappings: { "gh_oauth_client_id": "GitHub:ClientId" }
    /// Falls back to the `__` → `:` convention for unmapped keys.
    public Dictionary<string, string> Mappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
