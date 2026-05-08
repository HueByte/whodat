# Usage

Every command with a runnable example. For the full per-flag reference, see [cli.md](cli.md).

## Looking up someone

The most basic flow. Public, no auth needed.

```bash
whodat sleepless
```

This is shorthand for `whodat lookup sleepless`. Both work the same.

If `sleepless` has aliases set, you can look them up by any alias too:

```bash
whodat sl       # alias of sleepless, returns the same card
```

The output is rendered side-by-side with the avatar on the left, the blurb wrapped on the right, and metadata underneath. If the user has no avatar, the layout falls back to single-column.

A 404 means either no such handle/alias OR the user is hidden. The CLI prints the API's error and exits non-zero.

## Registering

Pick **one** auth method at registration time. You can't switch later without delete + re-register.

### With a password

```bash
whodat register sleepless \
  --text "building things, mass graveyard of side projects" \
  --avatar ./me.jpg \
  --meta github=HueByte \
  --meta site=huebyte.dev
```

Prompts:

```text
password:
confirm:
```

On success, prints `registered sleepless` and saves your bearer token to the local session file.

### With GitHub OAuth (device flow)

```bash
whodat register sleepless --github --text "shipping things"
```

The CLI prints a code and opens your browser to `github.com/login/device`:

```text
  open:  https://github.com/login/device
  code:  ABCD-1234

waiting for github authorization (Ctrl+C to cancel)...
```

Type the code on GitHub, click Authorize, and the CLI saves the token automatically. No password is set in this flow.

### With a profile file

If you keep your profile in dotfiles:

```bash
whodat register sleepless --github --profile ~/dotfiles/whodat.json
```

`whodat.json`:

```json
{
  "text": "building things, mass graveyard of side projects",
  "avatar": "./me.jpg",
  "metadata": { "github": "HueByte", "site": "huebyte.dev" }
}
```

Per-flag args still override the file:

```bash
# everything from the profile, but override the blurb just for this run
whodat register sleepless --github --profile ~/whodat.json --text "shipping today"
```

## Updating your entry

`set` is partial: only the fields you pass are changed.

### Text only

```bash
whodat set --text "currently: refactoring"
```

### Avatar from a URL

```bash
whodat set --avatar https://github.com/HueByte.png
```

The CLI fetches the image, renders to colored ASCII, uploads the rendered string. The server never stores the original image.

### Replace metadata

```bash
whodat set \
  --meta github=HueByte \
  --meta site=huebyte.dev \
  --meta keybase=hue
```

Note: `set` with `--meta` adds/overrides those keys, but does NOT touch keys you don't pass. If you want to wipe metadata entirely, send empty:

```bash
whodat set --profile <(echo '{ "metadata": {} }')
```

### Update everything from a profile

```bash
whodat set --profile ~/dotfiles/whodat.json
```

## Showing your own card

```bash
whodat me
```

This is auth-checked - if your token is invalid, you get 401, unlike `whodat <yourHandle>` which would still show your public card without proving anything.

## Aliases

Up to 5 alternate names that resolve to your primary handle.

```bash
whodat alias add sl
whodat alias add sleepy
whodat alias rm sleepy
whodat alias clear
```

Constraints:

- Same format as a handle: lowercase `[a-z0-9-]`, 2-32 chars
- Globally unique across all handles AND aliases
- Can't equal your own handle

After adding aliases, the renderer shows them under the handle:

```text
sleepless (registered 2026-05-08)
also: sl, sleepy
```

## Hiding / showing

`hide` makes your profile 404 to the public. Your bearer-checked `whodat me` still works.

```bash
whodat hide
whodat unhide
```

**Cascade:** going hidden also forces `RandomVisible=false` (so you don't appear in `whodat random`). Going unhidden does NOT auto-restore it. Re-opt explicitly with:

```bash
whodat discoverable
```

To stay public but opt out of random discovery:

```bash
whodat undiscoverable
```

Each visibility command prints your full card after applying the change so you can see the resulting state.

## Logging in on a new machine

You set up whodat on machine A. You're now on machine B and want the same identity.

```bash
whodat login --github
```

Same device-flow dance as register. Server recognizes your GitHub identity, rotates your bearer token (machine A's session now becomes invalid), saves the new token on machine B.

> Logging in is a deliberate "kick out other sessions" gesture. There's no concept of multiple concurrent sessions in v0.x.

## Deleting your account

```bash
whodat delete            # asks for confirmation
whodat delete --yes      # no prompt
```

Removes your user, all aliases, all login links. The handle becomes available for someone else immediately.

## Self-updating

Manual install? The CLI keeps itself current:

```bash
whodat update --check    # see what's available, don't download
whodat update            # download + replace in place
```

Output of `--check`:

```text
current: 0.2.1
latest:  0.2.1
→ up to date
```

Or:

```text
current: 0.2.0
latest:  0.2.1
→ update available - run `whodat update`
```

Choco / brew users update via their package manager (`choco upgrade whodat`, `brew upgrade --formula <url>`).

## Pointing at a different registry

Default is `https://whoisdat.dev`. Override with the `--api` flag for a single command:

```bash
whodat --api http://127.0.0.1:5099 me
```

Or persistently with an env var:

```bash
export WHODAT_API=https://my-self-hosted.example.com
whodat sleepless                 # talks to my-self-hosted.example.com
```

Useful when:

- You're developing locally and want to hit `localhost:5099`
- You're running a private whodat for your team
- You want to test a staging deployment

## A practical multi-machine flow

Day 1: laptop, want to register and ship a profile.

```bash
# laptop ~$
cat > ~/whodat.json <<'EOF'
{
  "text": "building things",
  "avatar": "https://github.com/HueByte.png",
  "metadata": { "github": "HueByte", "site": "huebyte.dev" }
}
EOF

whodat register hue --github --profile ~/whodat.json
whodat hue
```

Day 2: desktop, same identity.

```bash
# desktop ~$
whodat login --github            # paste the code, authorize, done
whodat me                        # confirms it's you
whodat set --text "now from the desktop"
```

Day 3: laptop again, refresh content.

```bash
# laptop ~$
whodat me                        # 401 because day-2 login rotated the token
whodat login --github            # log back in here
whodat set --text "synced"
```

This is the expected friction: one active session at a time, switch by re-logging-in. If/when multi-session support lands, this round-trip goes away.

## Quick reference card

```text
whodat <handle>                  Look up
whodat register <handle> [flags] Claim
whodat login [--github]          Switch session here
whodat me                        Self
whodat set [flags]               Edit
whodat alias add|rm|clear        Manage aliases
whodat hide / unhide             Public visibility
whodat discoverable / undiscoverable   Random discovery
whodat delete                    Tear it all down
whodat update [--check]          Self-upgrade
```
