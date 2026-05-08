# CLI reference

Single Rust binary. Cross-compiled for `x86_64`/`aarch64` × `linux-musl`/`apple-darwin` and `x86_64-pc-windows-msvc`. Talks to a whodat API over HTTPS.

## Install

| Platform | Command |
|---|---|
| Windows (Chocolatey) | `choco install whodat` |
| macOS / Linux (Homebrew formula by URL) | `brew install --formula https://raw.githubusercontent.com/HueByte/whodat/master/packaging/homebrew/whodat.rb` |
| Manual | grab a zip/tar.gz from [Releases](https://github.com/HueByte/whodat/releases) and drop the binary on `$PATH` |
| From source | `cargo install --path src/cli` |

## Updating

| Method | Command |
|---|---|
| Chocolatey | `choco upgrade whodat` |
| Homebrew (URL formula) | re-run the install command above with the new URL |
| Manual / self-update | `whodat update` |

`whodat update` pulls the latest GitHub Release, picks the asset matching this host's target triple, verifies SHA256, and replaces the running binary. `whodat update --check` reports current vs latest without downloading.

## Configuration

| Knob | How to set | Purpose |
|---|---|---|
| API base URL | `--api <url>` flag, or `WHODAT_API` env var, or default `https://whoisdat.dev` | Which whodat registry to query |

The flag takes priority over the env var, which takes priority over the compile-time default.

### Where the session token lives

After `register` or `login`, the CLI saves the bearer token to:

| Platform | Path |
|---|---|
| Linux | `~/.config/whodat/session.json` |
| macOS | `~/Library/Application Support/whodat/session.json` |
| Windows | `%APPDATA%\whodat\session.json` |

The file is plain JSON: `{ "handle": "...", "token": "wd_..." }`. Anything that can read it can act as you, so treat it like an SSH private key. Delete it (or run `whodat delete`) to log out.

## Commands

```text
whodat <handle>                  Look up a handle (alias for `whodat lookup`)
whodat lookup <handle>           Same - public, no auth needed
whodat register <handle> [...]   Claim a handle
whodat login [--github]          Re-authenticate on this machine (rotates token)
whodat me                        Show your own entry (auth-checked)
whodat set [...]                 Update your entry
whodat hide                      Make profile 404 to public lookups
whodat unhide                    Reverse `hide`
whodat discoverable              Allow appearance in `whodat random` (default)
whodat undiscoverable            Opt out of random discovery
whodat alias add <name>          Add a handle alias (max 5)
whodat alias rm <name>           Remove an alias
whodat alias clear               Drop all aliases
whodat delete [--yes]            Delete your registration
whodat update [--check]          Self-update from the latest GitHub Release
```

### `register <handle>`

Two auth modes - pick **one** at registration:

**Password** (default):

```bash
whodat register sleepless --text "..."
# prompts: password / confirm
```

**GitHub OAuth** (device flow):

```bash
whodat register sleepless --github --text "..."
# prints user_code, opens browser to github.com/login/device, polls until authorized
```

Auth mode is chosen *once* at registration and can't be changed later (without delete + re-register).

#### Optional flags

| Flag | Purpose |
|---|---|
| `--text "..."` | Free-text blurb (≤ 280 chars) |
| `--avatar <path or URL>` | Image to render as colored ASCII before upload |
| `--meta key=value` (repeatable) | Metadata key/value pairs |
| `--profile <file.json>` | Load text/avatar/metadata from JSON; per-flag values override per-key |

### `login --github`

Re-authenticates an existing user on this machine. Same device-flow dance as `register --github` but server figures out from the GitHub identity that you already exist, rotates your token, and returns the new bearer. **Logging in invalidates the previous token** - sessions on other machines stop working until they `login` too.

### `set [...]`

Partial update of your entry. Only fields you pass are changed; anything you omit stays put. Same flag set as `register` (text / avatar / meta / profile).

### `me`

Hits `/api/u/me` with your bearer token. Renders your entry. If your token is expired or revoked, you get an unambiguous 401 (vs the public lookup, which would still show your card).

### `hide` / `unhide`

`hide` flips `IsHidden=true` on your entry. After hiding:

- `whodat <yourHandle>` from anyone (including yourself unauthenticated) → 404
- `whodat me` (with your bearer) → still works
- Aliases also 404 - hiding the user hides every name pointing at them

`unhide` flips it back. **Cascade caveat:** going hidden also forces `RandomVisible=false`. Going unhidden does NOT auto-restore `RandomVisible` - that's a deliberate "re-opt to discovery" gate. Run `discoverable` to put yourself back in `whodat random` results.

### `discoverable` / `undiscoverable`

Independent of hide. Controls whether your handle can pop up in `whodat random` (planned). Hidden + discoverable doesn't make sense - the cascade above prevents that combination.

### `alias add / rm / clear`

Aliases are alternate names that resolve to your primary handle. Looking up an alias returns your card (and the renderer prints `also: foo, bar` under your handle).

Constraints:

- Max **5** aliases per user
- Same format as a handle: lowercase, `[a-z0-9-]`, 2-32 chars
- Globally unique across all handles AND aliases - can't use one someone else owns
- Can't alias your own handle

Server enforces all of the above; the CLI surfaces the error message verbatim.

### `delete`

Removes your registration entirely - user row, all aliases, all login links. Your token becomes invalid. Prompts for confirmation unless you pass `--yes`.

### `update [--check]`

Self-update from the latest GitHub Release. With `--check`, just prints current/latest versions; without, downloads and replaces the running binary in-place.

## The `profile.json` file

Both `register` and `set` accept `--profile <file>`. Format:

```json
{
  "text": "building things, mass graveyard of side projects",
  "avatar": "https://github.com/HueByte.png",
  "metadata": {
    "github": "HueByte",
    "site": "huebyte.dev"
  }
}
```

All three top-level keys are optional. Per-flag CLI args still win:

```bash
# loads everything from the file, but overrides text just for this run
whodat set --profile ~/dotfiles/whodat.json --text "shipping today"

# explicit --meta merges with file metadata, explicit keys win on conflicts
whodat set --profile ~/dotfiles/whodat.json --meta site=alt.example.com
```

`avatar` accepts either a local path or an http(s) URL. The CLI loads the bytes, renders to colored ASCII (40-col block characters with 24-bit ANSI), and uploads the rendered string - the server never stores the image itself.

## Examples

```bash
# Public lookup
whodat sleepless

# Look up via alias - same card
whodat sl

# Register, password mode, with avatar from local file
whodat register myhandle \
  --text "shipping things" \
  --avatar ./me.jpg \
  --meta github=mygh \
  --meta site=mysite.dev

# Register via GitHub
whodat register myhandle --github --profile ~/whodat.json

# Update only the blurb
whodat set --text "now with refactored README"

# Add an alias
whodat alias add my

# Hide entirely (404 to the public, /me still works for you)
whodat hide

# Bring back, but stay un-discoverable
whodat unhide
# (RandomVisible still false; explicitly opt back in below)
whodat discoverable

# Self-update
whodat update
```

## Exit codes

| Code | Meaning |
|---|---|
| `0` | success |
| non-zero | error (printed to stderr; specifics depend on the failure) |

The CLI doesn't currently use granular exit codes. PRs welcome.
