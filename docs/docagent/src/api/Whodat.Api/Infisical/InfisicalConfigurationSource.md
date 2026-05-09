# InfisicalConfigurationSource

> **File:** `src/api/Whodat.Api/Infisical/InfisicalConfigurationSource.cs`  
> **Kind:** class

InfisicalConfigurationSource is a configuration source that carries InfisicalOptions and constructs an InfisicalConfigurationProvider used by the configuration system.

## Remarks

Exposes a read-only Options property initialized via the primary constructor, and its Build method returns a new InfisicalConfigurationProvider bound to this source. This class acts as a thin glue between InfisicalOptions and the configuration provider, allowing Infisical settings to participate in the Microsoft.Extensions.Configuration pipeline.

## Example

```csharp
using Microsoft.Extensions.Configuration;

// Configure Infisical options
var options = new InfisicalOptions
{
    // populate as needed
};

// Create the source and attach it to the builder
var builder = new ConfigurationBuilder();
builder.Add(new InfisicalConfigurationSource(options));

IConfiguration config = builder.Build();

// Access configuration values as usual
string value = config["SomeSetting"];
```

## Notes

- The Options property is read-only and is initialized from the constructor; there is no null-check in the constructor, so ensure a valid InfisicalOptions is provided.
- Build creates a new InfisicalConfigurationProvider bound to this source each time it is called.
- This symbol relies on InfisicalConfigurationProvider and integrates Infisical options into the configuration system via IConfigurationSource.