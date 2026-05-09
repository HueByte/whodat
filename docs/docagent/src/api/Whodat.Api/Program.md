# Program

> **File:** `src/api/Whodat.Api/Program.cs`  
> **Kind:** file

Bootstraps the ASP.NET Core WebApplication for the Whodat API, wiring up configuration, logging, data access, authentication, OpenAPI, endpoints, and startup migrations before starting the host.

## Remarks
- Infisical integration runs early in the configuration pipeline via builder.Configuration.AddInfisical, allowing secrets to override appsettings.json and environment variables; when Infisical:Enabled is false this is a no-op.
- Serilog is configured to pull settings from the app configuration and DI services, providing structured logging and request logging throughout the request pipeline.
- The SQLite DbContext (WhodatDb) is registered with a configurable data source path (Whodat:DbPath defaults to whodat.db).
- Identity is configured in a slim form (IdentityCore) using UserManager without SignInManager or cookie-based flows; specific username constraints and password requirements are customized.
- Bearer token authentication is registered with a dedicated handler (BearerTokenHandler) and its scheme name, enabling stateless API authentication.
- GitHub-related options are loaded from the GitHub section and a typed HttpClient is registered for GitHub interactions with JSON acceptance, a custom User-Agent, and a 10-second timeout.
- Forwarded headers are enabled to support reverse proxies (X-Forwarded-For and X-Forwarded-Proto), and known networks/proxies are cleared to accommodate dynamic container networking.
- On startup, a scope is created to apply EF Core migrations (db.Database.Migrate) to ensure the schema is up-to-date; EnsureCreated is deliberately avoided to prevent silent schema drift.
- OpenAPI (Swagger) endpoints are mapped only in development, keeping production lean.
- API endpoints are grouped under /api, with a health endpoint at /api/health and additional endpoint mappings via UsersEndpoints and GithubAuthEndpoints.
- The host startup is wrapped in a try/catch/finally block: fatal logging occurs on unhandled exceptions, and Log.CloseAndFlush is called on exit to flush logs.
- This file serves as the single, centralized startup path for configuring the application, including DI registrations, middleware, and endpoint registration.

## Example
```csharp
// Example: verify the health endpoint after the API has started
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
var resp = await client.GetAsync("/api/health");
var content = await resp.Content.ReadAsStringAsync();
Console.WriteLine(content);
```

## Notes
- Port and base URL depend on runtime configuration; the health endpoint is available at /api/health when the server is running.
- Behind reverse proxies, ensure proper forwarding is configured to expose client IPs and scheme correctly via forwarded headers.
- If migrations fail during startup, the host will terminate with a fatal log entry, so inspect the exception and migration status.
- The OpenAPI UI is only mapped in development environments to avoid exposing schemas in production.
- Secret injection via Infisical happens before other configuration reads, enabling overrides at startup without changing code.
