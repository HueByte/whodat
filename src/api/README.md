# whodat API

ASP.NET Core 10 + SQLite. Stateless registry behind nginx; single `Users` table; bearer-token auth for mutations. Part of the [whodat](../../README.md) project — see the root README for the install / hosting story.

## Endpoints

| Verb | Route | Auth | What |
|---|---|---|---|
| GET  | `/api/health` | — | Liveness probe |
| GET  | `/api/u/{handle}` | — | Public lookup |
| POST | `/api/register` | — | Password registration → returns bearer token |
| PUT  | `/api/u/me` | bearer | Partial update of `text`, `avatar_ascii`, `metadata` |
| DELETE | `/api/u/me` | bearer | Removes the registration |
| POST | `/api/auth/github/start` | — | Begin GitHub device flow (returns `user_code` + `verification_uri`) |
| POST | `/api/auth/github/complete` | — | Polled by the CLI; returns the bearer token once GitHub authorizes |

OpenAPI doc is exposed at `/openapi/v1.json` in `Development` only.

## Run locally

```bash
# from src/api/
cp Whodat.Api/appsettings.json.example Whodat.Api/appsettings.json
dotnet run --project Whodat.Api
```

The API listens on `http://localhost:5260` by default (driven by [Properties/launchSettings.json](Whodat.Api/Properties/launchSettings.json)). Override with `ASPNETCORE_URLS` or `--urls`.

For Docker / nginx / production, see [the root README](../../README.md#host-your-own-registry).

## External services

The API integrates with up to two external services. All keys are optional — the API runs with both disabled (no GitHub auth, secrets sourced only from `appsettings.json` + env vars).

| Name | ConfigNames | Description | Link |
|---|---|---|---|
| GitHub OAuth App | `GitHub:ClientId` | Client ID of an OAuth App with **Enable Device Flow** turned on. Powers `/api/auth/github/*`. The client secret is **not** needed (device flow only). Leave blank to disable; the endpoints return 503. | <https://github.com/settings/developers> |
| Infisical key vault | `Infisical:Enabled`, `Infisical:SiteUrl`, `Infisical:ProjectId`, `Infisical:EnvironmentSlug`, `Infisical:SecretPath`, `Infisical:ClientId`, `Infisical:ClientSecret`, `Infisical:Mappings` | Optional. When enabled, the [InfisicalConfigurationProvider](Whodat.Api/Infisical/InfisicalConfigurationProvider.cs) pulls remaining secrets at startup over Universal Auth and merges them into `IConfiguration`, overriding `appsettings.json`. | <https://infisical.com> |

## Vault secret naming

Vault-side secret names use `snake_case` (cross-project consistency). The `Infisical:Mappings` dictionary translates each one to its idiomatic .NET config key. Current map:

| Vault key | .NET config key | Used by |
|---|---|---|
| `gh_oauth_client_id` | `GitHub:ClientId` | GitHub OAuth endpoints |

Append a line to `Mappings` in `appsettings.json` whenever you add a vault key that needs to land in a sectioned config path. Unmapped vault keys still pass through with the `__` → `:` convention (so `GitHub__ClientId` would also work).

## Configuration order

Sources are evaluated low-to-high priority; later sources win:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets (Development only)
4. Environment variables
5. Command-line args
6. **Infisical** *(only when `Infisical:Enabled=true`)*

So Infisical is canonical when active. To override a single value at runtime without redeploying secrets, pass it on the command line.

## Layout

```text
Whodat.Api/
  Auth/          BCrypt password hashing, bearer-token helpers, GitHub options binding
  Data/          EF Core DbContext (SQLite, single Users table, WAL mode)
  Endpoints/     Minimal-API endpoint groups (UsersEndpoints, GithubAuthEndpoints)
  Infisical/     IConfigurationProvider that fetches secrets via the Infisical SDK
  Models/        UserEntry entity + request/response DTOs
  Properties/    launchSettings.json (dev only)
  Program.cs     Host setup, Serilog, ForwardedHeaders, route-group wiring
Dockerfile       Multi-stage build on dotnet/sdk + dotnet/aspnet (linux/amd64,arm64)
.dockerignore    Excludes bin/obj/logs/db
```
