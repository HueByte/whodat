# Deployment

How to run your own whodat registry.

## TL;DR

```bash
git clone https://github.com/HueByte/whodat
cd whodat
cp .env.example .env       # edit: WHODAT_PORT and (optionally) GITHUB_CLIENT_ID + INFISICAL_*
docker compose up -d
```

That brings up two containers:

- `whodat-api` (the .NET API, internal-only on the docker network)
- `whodat-nginx` (the public face; binds the host port)

…both connected via a private docker network. The API is **not** exposed on a host port directly - all outside traffic goes through nginx.

## What the compose file gives you

[`docker-compose.yml`](../docker-compose.yml) at the repo root:

| Service | Image | Listens on | Volumes |
|---|---|---|---|
| `api` | `ghcr.io/huebyte/whodat-api:latest` (multi-arch) | port 8080 inside the docker network | `whodat-data` (SQLite + WAL files), `whodat-logs` (Serilog rolling logs) |
| `nginx` | `nginx:1.27-alpine` | `${WHODAT_PORT:-8080}` on the host | bind-mounts `infra/nginx/default.conf` and `infra/nginx/html/` |

`nginx` `depends_on: api: condition: service_healthy` - it won't start until the API's `/api/health` is returning 200 inside the docker network.

## Configuration knobs

All set via environment variables (or `.env` next to `docker-compose.yml`).

### Public port

```env
WHODAT_PORT=80           # whatever port nginx should bind
```

For a real deployment, terminate TLS in front (Cloudflare, Traefik, another nginx). The included nginx is a plain HTTP reverse proxy - it doesn't carry certs.

### GitHub OAuth (optional)

Required only if you want `whodat register --github` and `whodat login --github` to work. Disable by leaving blank - the relevant endpoints return 503.

```env
GITHUB_CLIENT_ID=Iv1.abc123...
```

To get a Client ID: https://github.com/settings/developers → OAuth Apps → New OAuth App. **Toggle "Enable Device Flow"** on the app's settings page after creation - without that, GitHub rejects device requests with `incorrect_client_credentials` and the API surfaces it as 502.

The client *secret* is **not** needed (device flow only). The "Authorization callback URL" field is required by GitHub's form but never invoked - put any valid URL.

### Database path

```env
Whodat__DbPath=/data/whodat.db    # default, lives in the whodat-data volume
```

Schema is applied on container start via `db.Database.Migrate()`. Migrations are checked into the repo at [`src/api/Whodat.Api/Data/Migrations/`](../src/api/Whodat.Api/Data/Migrations/) - the container picks them up automatically. No `EnsureCreated`, no schema drift.

### Logging

Serilog config lives in [`appsettings.json`](../src/api/Whodat.Api/appsettings.json.example). Default sinks: Console (visible via `docker compose logs api`) and a rolling file at `/app/logs/whodat-YYYYMMDD.log` retained for 14 days, kept on the `whodat-logs` named volume.

To override, mount your own `appsettings.json` over `/app/appsettings.json` or use environment variables (`Serilog__MinimumLevel__Default=Debug`, etc.).

## Infisical secret store

Optional. When enabled, the API pulls remaining secrets from your Infisical project at startup, so `.env` only needs the bootstrap creds.

### What you need

- An Infisical instance (cloud at `app.infisical.com` or self-hosted, e.g. `secrets.voidcube.cloud`)
- A project (slug = whatever you set, e.g. `whodat`)
- A **machine identity** with Universal Auth, attached to the project as Viewer or Member
- The identity's Client ID and Client Secret (the secret is shown once at creation - copy it)

### Wiring

```env
INFISICAL_ENABLED=true
INFISICAL_SITE_URL=https://app.infisical.com
INFISICAL_PROJECT_ID=...
INFISICAL_ENVIRONMENT=prod
INFISICAL_SECRET_PATH=/
INFISICAL_CLIENT_ID=...
INFISICAL_CLIENT_SECRET=...
```

### Naming convention for vault secrets

Secret keys in Infisical use `__` as the section separator, mirroring ASP.NET Core's env-var convention. So a secret named `GitHub__ClientId` in your vault populates `Configuration["GitHub:ClientId"]` automatically. No mapping table needed.

### Priority

Configuration sources from low to high:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. Command-line args
5. **Infisical** (when enabled - added last; overrides everything above except cmd args)

So Infisical is the canonical source in production. Use cmd args for emergency one-off overrides.

### Identity scoping (free tier)

Free-tier Infisical doesn't support custom roles, so you can't restrict an identity to a specific path. Just add the identity as **Viewer** on the project - it can read everything in the project, but can't modify anything.

For multi-environment hygiene (separate dev/staging/prod creds, separate identities per stage), use multiple identities scoped to one project each. Or upgrade to a paid tier and use custom roles per env/path.

## CI secrets via Infisical (optional but nice)

Same Infisical project, separate folder. Put your `CHOCOLATEY_API_KEY` (and any other CI-time secret) under `prod:/ci`. The release workflow's `choco` job pulls them via `infisical/secrets-action`:

```yaml
- uses: infisical/secrets-action@v1.0.7
  with:
    method: universal
    client-id:     ${{ secrets.INFISICAL_CLIENT_ID }}
    client-secret: ${{ secrets.INFISICAL_CLIENT_SECRET }}
    domain:        https://secrets.voidcube.cloud
    project-slug:  whodat
    env-slug:      prod
    secret-path:   /ci
    export-type:   env
```

After the action runs, every secret in that folder is exposed as an env var on the runner - `$env:CHOCOLATEY_API_KEY` is then available to the next step.

The only GitHub-managed secrets you need: `INFISICAL_CLIENT_ID` and `INFISICAL_CLIENT_SECRET`. Everything else can move to Infisical and rotate centrally.

## Putting TLS in front

The bundled nginx serves plain HTTP. For real deployments:

### Option A - Cloudflare (easiest)

Point your domain at the host, enable Cloudflare in front, set SSL/TLS mode to "Flexible" or (better) "Full". Cloudflare terminates TLS, talks plain HTTP to your nginx.

### Option B - Traefik / Caddy in front

Add a Traefik or Caddy container that terminates TLS using ACME, reverse-proxies to the existing `nginx` container's port. The bundled `nginx` then becomes purely "static + path-routing inside the container network."

### Option C - Run a separate nginx with cert-bot on the host

Stop the bundled `nginx` (just remove that block from `docker-compose.yml`). Map the host port directly to the API container, set up a host-level nginx with letsencrypt.

Pick whichever fits your existing stack. The compose file is opinionated only about "the API doesn't bind a host port" - everything else is swappable.

## Upgrading

```bash
docker compose pull
docker compose up -d
```

Both images get pulled (the API at `:latest`, nginx at the pinned `1.27-alpine`). EF migrations apply automatically on container start - no manual `dotnet ef database update`, no volume wipe needed for ordinary schema changes.

To pin a specific API version, change `image: ghcr.io/huebyte/whodat-api:latest` to `:v0.2.1` (or whichever) in your local copy of `docker-compose.yml`. Image tags follow the GitHub Release tags 1:1.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `/api/auth/github/start` returns 502 | "Enable Device Flow" not toggled on the OAuth App |
| `/api/auth/github/start` returns 503 | `GitHub:ClientId` empty in config - check env var or Infisical pull |
| All endpoints 500 with `no such table: AspNetUsers` | Old DB from pre-Identity schema. Wipe the volume once: `docker compose down && docker volume rm whodat_whodat-data && docker compose up -d` |
| nginx healthcheck fails | API isn't reachable on the docker network. `docker compose logs api` to see why it's not listening |
| Choco publish job 401 | Infisical credentials wrong/expired - regenerate the machine identity's Client Secret, update GH repo secrets |

For everything else, `docker compose logs api --tail 100` shows the Serilog stream - the exception trace is usually self-explanatory.
