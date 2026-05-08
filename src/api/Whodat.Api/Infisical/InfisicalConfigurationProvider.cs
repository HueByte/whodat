using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.Configuration;

namespace Whodat.Api.Infisical;

/// Pulls secrets from an Infisical project and exposes them as configuration
/// keys. Naming convention: secret keys use `__` as the section separator, the
/// same way ASP.NET Core treats environment variables. So a secret named
/// `GitHub__ClientId` in Infisical becomes `Configuration["GitHub:ClientId"]`.
public class InfisicalConfigurationProvider(InfisicalConfigurationSource source) : ConfigurationProvider
{
    public override void Load()
    {
        var opts = source.Options;
        if (!opts.Enabled) return;

        if (string.IsNullOrWhiteSpace(opts.ProjectId))
            throw new InvalidOperationException("Infisical:ProjectId is required when Infisical:Enabled=true");
        if (string.IsNullOrWhiteSpace(opts.ClientId) || string.IsNullOrWhiteSpace(opts.ClientSecret))
            throw new InvalidOperationException("Infisical:ClientId and Infisical:ClientSecret are required when Infisical:Enabled=true");

        var settings = new InfisicalSdkSettingsBuilder()
            .WithHostUri(opts.SiteUrl)
            .Build();

        var infisical = new InfisicalClient(settings);

        infisical.Auth().UniversalAuth()
            .LoginAsync(opts.ClientId!, opts.ClientSecret!)
            .GetAwaiter().GetResult();

        var secrets = infisical.Secrets()
            .ListAsync(new ListSecretsOptions
            {
                ProjectId = opts.ProjectId!,
                EnvironmentSlug = opts.EnvironmentSlug,
                SecretPath = opts.SecretPath,
                Recursive = opts.Recursive,
            })
            .GetAwaiter().GetResult();

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in secrets)
        {
            // Explicit Mappings win; otherwise fall back to the env-var
            // convention where `__` becomes the section separator.
            var key = opts.Mappings.TryGetValue(s.SecretKey, out var mapped)
                ? mapped
                : s.SecretKey.Replace("__", ConfigurationPath.KeyDelimiter);
            data[key] = s.SecretValue;
        }
        Data = data;
    }
}
