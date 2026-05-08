using Microsoft.Extensions.Configuration;

namespace Whodat.Api.Infisical;

public static class InfisicalConfigurationExtensions
{
    public const string SectionName = "Infisical";

    /// Adds the Infisical configuration source if `Infisical:Enabled` is true.
    /// Reads connection info from the existing configuration chain (so you can
    /// set `Infisical__ClientId` etc. via env vars without redeploying secrets).
    /// Throws on missing required fields when enabled.
    public static IConfigurationBuilder AddInfisical(
        this IConfigurationBuilder builder,
        IConfiguration baseConfig)
    {
        var opts = new InfisicalOptions();
        baseConfig.GetSection(SectionName).Bind(opts);
        if (!opts.Enabled) return builder;

        return builder.Add(new InfisicalConfigurationSource(opts));
    }
}
