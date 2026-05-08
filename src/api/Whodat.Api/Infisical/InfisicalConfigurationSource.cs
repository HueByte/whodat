using Microsoft.Extensions.Configuration;

namespace Whodat.Api.Infisical;

public class InfisicalConfigurationSource(InfisicalOptions options) : IConfigurationSource
{
    public InfisicalOptions Options { get; } = options;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new InfisicalConfigurationProvider(this);
}
