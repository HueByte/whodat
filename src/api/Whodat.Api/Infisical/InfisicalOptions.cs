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
}
