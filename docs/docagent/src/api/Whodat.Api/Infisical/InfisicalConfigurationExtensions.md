# InfisicalConfigurationExtensions

> **File:** `src/api/Whodat.Api/Infisical/InfisicalConfigurationExtensions.cs`  
> **Kind:** class

Adds Infisical configuration to the host configuration pipeline when the Infisical:Enabled flag is true, by binding the Infisical section from the provided base configuration and wiring a dedicated InfisicalConfigurationSource into the builder. This design allows Infisical credentials to be supplied via the existing configuration chain (for example via environment variables like Infisical__ClientId) without redeploying secrets.

## Remarks

Reads Infisical options from the base configuration using the SectionName constant, so you can override values through the configuration providers already in use. If Enabled is false, the method returns the original builder unchanged. When enabled, it adds a new InfisicalConfigurationSource constructed from the bound options; missing required fields will trigger an exception during configuration.

## Example

```csharp
// Example: conditionally wire Infisical configuration based on existing configuration
var baseConfig = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var builder = new ConfigurationBuilder();

// Only adds Infisical if Infisical:Enabled is true in baseConfig
builder.AddInfisical(baseConfig);

var config = builder.Build();
```

## Notes
- If Infisical:Enabled is false, the builder is returned unchanged.
- The binding reads from the Infisical section of baseConfig, enabling env-var overrides via the standard `Infisical__<Property>` naming.
- When enabled, an InfisicalConfigurationSource backed by the bound InfisicalOptions is added to the configuration pipeline.
- The method is stateless and thread-safe; it constructs fresh options and a new configuration source per call.