# Development

How to work on whodat - local dev loop, the release flow, schema migrations, adding features.

## Prerequisites

- **Rust** 1.95+ (`rustup install stable`) for the CLI
- **.NET 10 SDK** for the API
- **Docker** + `docker compose v2` for the full-stack run and image build
- **dotnet-ef tool** for migrations: `dotnet tool install --global dotnet-ef`

## Local dev loop

### CLI

```bash
cd src/cli
cargo run -- <handle>                              # talks to the default whoisdat.dev
WHODAT_API=http://127.0.0.1:5099 cargo run -- me   # local API
```

Daily commands while developing:

```bash
cargo fmt              # apply rustfmt
cargo clippy --all-targets -- -D warnings    # lint, exact CI command
cargo build --release  # produce target/release/whodat
cargo test --release   # currently no tests (PRs welcome)
```

### API

```bash
cp src/api/Whodat.Api/appsettings.json.example src/api/Whodat.Api/appsettings.json
cd src/api
dotnet run --project Whodat.Api --no-launch-profile
```

By default that listens on `http://localhost:5260` (overridden via `ASPNETCORE_URLS` or `--urls`). The first run creates `Whodat.Api/whodat.db` in the project directory - relative path, change `Whodat:DbPath` in `appsettings.json` if you want it elsewhere.

```bash
dotnet format src/api/Whodat.slnx --verify-no-changes   # exact CI format check
dotnet build src/api/Whodat.slnx                        # build
dotnet test  src/api/Whodat.slnx                        # currently no tests
```

### Full stack (docker compose)

```bash
docker compose up -d --build
docker compose logs -f api
```

The bundled stack listens on `${WHODAT_PORT:-8080}`. Useful when you want to test the nginx layer or simulate the production wiring.

## Branching model

```
dev branch          ← daily work happens here (push freely)
   │
   │ PR (when ready to release)
   ▼
master branch       ← only updated via PR; each merge = a release
```

PRs targeting master must bump `version` in [src/cli/Cargo.toml](../src/cli/Cargo.toml) - the `release-checklist.yml` workflow rejects PRs that don't.

Push directly to dev for daily work. Open a PR to master when you're ready to ship.

## Workflows that fire on each event

| Event | Workflows | Purpose |
|---|---|---|
| push to `dev` | `ci.yml` | fmt + clippy + build + test (Rust + .NET) + markdown lint |
| PR `dev → master` | `ci.yml`, `release-checklist.yml`, `docker.yml` | full validation; docker.yml builds the image but doesn't push |
| push to `master` (i.e. PR merge) | `ci.yml`, `docker.yml`, `release.yml` | docker pushes `:vX.Y.Z` and `:latest`; release.yml cuts the GH release + binaries + choco + brew |

`release-checklist.yml` is the gate - if Cargo.toml version matches master's version, it fails the PR. That's the forcing function for every merge to master = a real release.

## The release pipeline in detail

When `release.yml` fires on a master push:

```
detect      reads Cargo.toml version, checks if release exists, sets should_release
   │
   ▼
prepare     gh release create vX.Y.Z --draft (creates the tag + draft release)
   │
   ▼
build       5-job matrix: linux-musl x64+arm64, macos x64+arm64, windows msvc
            uses taiki-e/upload-rust-binary-action; uploads each binary +
            .sha256 file to the draft release
   │
   ▼
publish     generates release notes from `git log <prev-tag>..HEAD`
            gh release edit --notes-file ... --draft=false (release goes public)
   │
   ├──────► choco        downloads windows zip, computes sha256, stamps the
   │                     .nuspec + chocolateyInstall.ps1 templates,
   │                     `choco pack` and `choco push` to community.chocolatey.org
   │                     CHOCOLATEY_API_KEY pulled from Infisical via secrets-action
   │
   └──────► homebrew     downloads tar.gz files, computes sha256s, renders
                         packaging/homebrew/whodat.rb from .template,
                         commits back to master as `chore(homebrew): bump formula to vX.Y.Z`
```

Rough wall-clock: 5-10 minutes from merge to "users can `whodat update`."

### If `choco` fails

Common cause: Infisical auth (machine identity creds wrong, secret moved, etc.). The release itself succeeds (binaries are public, brew formula is bumped) - only the choco push is missing.

Manual rescue: **Actions tab → Choco publish (manual) → Run workflow → tag: `vX.Y.Z`**. The workflow [`choco-publish.yml`](../.github/workflows/choco-publish.yml) re-runs only the choco bits for an existing tag, with a skip-if-already-published check so it's safe to re-run.

## Schema migrations

EF Core migrations are checked into [`src/api/Whodat.Api/Data/Migrations/`](../src/api/Whodat.Api/Data/Migrations/) and applied automatically on container start via `db.Database.Migrate()`.

### Adding a migration

```bash
cd src/api/Whodat.Api

# 1. Edit the model (e.g. add a property to WhodatUser, add a new entity)

# 2. Generate the migration
dotnet ef migrations add <DescriptiveName> --output-dir Data/Migrations

# 3. Inspect Data/Migrations/<timestamp>_<DescriptiveName>.cs - sanity-check the SQL

# 4. git add Data/Migrations/ + your model changes
```

The next deploy applies it on startup. No manual `dotnet ef database update`.

### Rolling back during dev

```bash
dotnet ef migrations remove           # if not yet applied to your local DB
dotnet ef database update <PreviousMigrationName>   # rollback after applied
```

### When EF can't auto-handle a change

Pure additive (new column with a default, new table, new index) → generated migration just works.

Data transformation (rename column, split column, backfill from another field) → the generated migration creates the structural change but doesn't move data. Edit the generated `.cs` file to add `migrationBuilder.Sql("UPDATE ... SET ...")` calls in the `Up()` method.

Always test against a fresh DB locally before committing:

```bash
rm $TEMP/whodat-test.db
Whodat__DbPath=$TEMP/whodat-test.db dotnet run --project Whodat.Api
```

## Adding a new HTTP endpoint

1. Decide the path under `/api/...` and whether it's authed
2. Add the handler in the relevant `Endpoints/*.cs` file (or create a new one)
3. Wire it in `Program.cs` via the `MapGroup("/api")` block, with `.RequireAuthorization()` if it needs auth
4. Add request/response DTOs to [`Models/Dtos.cs`](../src/api/Whodat.Api/Models/Dtos.cs) - use `[JsonPropertyName(...)]` for snake_case field names on the wire
5. Update [docs/api.md](api.md) with the new endpoint

If you need new EF entities or columns, generate a migration ([above](#adding-a-migration)).

## Adding a new CLI command

1. Add the variant to the `Command` enum in [`main.rs`](../src/cli/src/main.rs) with clap attributes
2. Create `src/cli/src/commands/<your_command>.rs` with a `run(...)` function
3. Add `pub mod <your_command>;` to `commands/mod.rs`
4. Wire the match arm in `main.rs`'s `match (cli.command, cli.handle)` block
5. Add new HTTP method to `api.rs` if the command talks to the server
6. Update [docs/cli.md](cli.md)

## Versioning

Semver, with one calibration:

| Bump | When |
|---|---|
| Patch (`X.Y.Z → X.Y.Z+1`) | Bug fixes, internal refactors, dependency bumps |
| Minor (`X.Y.0 → X.(Y+1).0`) | New features, additive schema changes, additional CLI commands |
| Major (`X.0.0 → (X+1).0.0`) | Breaking changes - wire format breaks, removed endpoints, removed CLI commands |

Pre-1.0 we're treating the wire format as still-mutable, so most additions are minor and only true breakages would be major. The choco/brew package versions follow the GitHub Release tag exactly - bumping Cargo.toml is what drives the entire fan-out.

## Updating dependencies

```bash
# Rust side
cd src/cli
cargo update
cargo clippy --all-targets -- -D warnings   # catch breaking lints

# .NET side
cd src/api
dotnet outdated -u    # if you've installed dotnet-outdated tool
# or manually: dotnet add Whodat.Api/Whodat.Api.csproj package <PackageName>
```

Bump in a small PR with a focused commit message - easier to bisect later if something regresses.

## Code style

| Language | Tool | Config |
|---|---|---|
| Rust | rustfmt (default), clippy `-D warnings` | rust 2021 edition, no custom rustfmt.toml |
| C# | `dotnet format` (.editorconfig + analyzer rules) | [`.editorconfig`](../.editorconfig) |
| Markdown | markdownlint-cli2 | [`.markdownlint.json`](../.markdownlint.json) |
| Line endings | LF everywhere | enforced via [`.gitattributes`](../.gitattributes) |

CI runs all of these. Run them locally before pushing to avoid round-trips.

## Pull request hygiene

- **One concept per PR** - easier to review, easier to revert if something breaks
- **Bump Cargo.toml version on PRs that should ship a release**, leave it alone on no-release PRs (CI fixes, doc tweaks). Bundling a doc-tweak with a feature PR is fine.
- **Migration files always go with the matching model change** in the same commit - never split them across commits, that creates a window where the model expects a column the schema doesn't have.
- **Smoke-test locally** for changes that touch the API or migrations: register/lookup/update/delete cycle on a fresh DB.

The PR template ([`.github/PULL_REQUEST_TEMPLATE.md`](../.github/PULL_REQUEST_TEMPLATE.md)) has the checklist; tick the boxes.
