# HTTP API reference

Base path: `/api`. Authentication: `Authorization: Bearer <token>` for endpoints that mutate or expose private data. Public lookups need nothing.

All bodies are JSON. All timestamps are unix seconds (UTC).

## Endpoints at a glance

| Verb | Path | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/health` | - | Liveness probe |
| `GET` | `/api/u/{handle}` | - | Public lookup; resolves aliases |
| `POST` | `/api/register` | - | Password registration |
| `GET` | `/api/u/me` | bearer | Auth-checked self-lookup |
| `PUT` | `/api/u/me` | bearer | Partial update |
| `DELETE` | `/api/u/me` | bearer | Remove account |
| `POST` | `/api/auth/github/start` | - | Begin device-flow OAuth |
| `POST` | `/api/auth/github/complete` | - | Polled until GH authorizes; returns token |

## `EntryDto` - the shape returned for every entry-shaped response

```json
{
  "handle":         "sleepless",
  "text":           "building things",
  "avatar_ascii":   "[38;2;…m▀▀▀…[0m\n…",
  "metadata":       { "github": "HueByte", "site": "huebyte.dev" },
  "aliases":        ["sl", "sleepy"],
  "is_hidden":      false,
  "random_visible": true,
  "registered_at":  1778266212,
  "updated_at":     1778266213
}
```

`avatar_ascii` is the already-rendered terminal string with embedded 24-bit ANSI escapes. Renderer-side, you split on `\n` and write each line. The server never stores the original image; ASCII-rendering happens once on the CLI side at upload time.

## `GET /api/health`

```http
GET /api/health
→ 200 { "status": "ok" }
```

Used by Docker's `HEALTHCHECK` and the nginx `/nginx-health` parallel.

## `GET /api/u/{handle}`

Public lookup. Resolves first by primary handle, then by alias. Hidden users return 404 either way - the existence of the handle isn't surfaced when hidden.

```http
GET /api/u/sleepless
→ 200 EntryDto       (resolved by handle)
→ 200 EntryDto       (resolved by alias - handle in response is the canonical primary)
→ 404                (no such handle/alias, OR hidden)
→ 400 { error: "invalid handle" }   (malformed handle)
```

Handle format: lowercase `[a-z0-9-]`, 2-32 chars, can't start or end with a hyphen.

## `POST /api/register`

Password-based registration. For GitHub registrations see `/auth/github/*` below.

```http
POST /api/register
Content-Type: application/json

{
  "handle":        "sleepless",      // required, validated
  "password":      "hunter2",        // required, ≥ 6 chars (Identity default)
  "text":          "...",            // optional, ≤ 280 chars
  "avatar_ascii":  "[…",        // optional, ≤ 64 KB
  "metadata":      { "k": "v" }      // optional
}

→ 200 { "token": "wd_…", "handle": "sleepless" }
→ 400 { error: "..." }                            // invalid handle/password/text/avatar
→ 409 { error: "handle taken" }
```

Password is hashed via Identity's `PasswordHasher<TUser>` (PBKDF2 by default). The bearer `token` is shown **once** in the response - store it (the CLI does this in `~/.config/whodat/session.json`).

## `GET /api/u/me`

Auth-checked self-lookup. Returns your own `EntryDto` regardless of `is_hidden`.

```http
GET /api/u/me
Authorization: Bearer wd_…

→ 200 EntryDto
→ 401                (no/invalid bearer)
```

Useful as a token-validity probe - if a stale token would 401 here, the public lookup wouldn't tell you.

## `PUT /api/u/me`

Partial update. Any field omitted from the body stays unchanged.

```http
PUT /api/u/me
Authorization: Bearer wd_…
Content-Type: application/json

{
  "text":           "...",            // optional, ≤ 280 chars
  "avatar_ascii":   "[…",       // optional, ≤ 64 KB; "" wipes
  "metadata":       { ... },          // optional, replace-all; {} wipes
  "is_hidden":      true,             // optional
  "random_visible": false,            // optional
  "aliases":        ["a", "b", "c"]   // optional; replace-all, max 5
}

→ 200 EntryDto                                        (with the updated fields reflected)
→ 401                                                 (no/invalid bearer)
→ 400 { error: "text too long" }                      (or other validation msg)
→ 400 { error: "max 5 aliases" }                      (alias cap exceeded)
→ 400 { error: "invalid alias 'X'" }                  (format check failed)
→ 400 { error: "alias 'X' equals your handle" }       (self-collision)
→ 409 { error: "an alias collides with another user's handle" }
→ 409 { error: "alias 'X' is taken" }                 (collides with another user's alias)
```

### Cascade rules

- Setting `is_hidden=true` **also** forces `random_visible=false` (server-side cascade - preserves the user's explicit preference across hide/unhide cycles).
- Setting `is_hidden=false` does NOT restore `random_visible`. The user must explicitly send `random_visible=true` to opt back into discovery.

### Aliases - replace-all semantics

Sending `"aliases": [...]` replaces the entire list. Add/remove operations on the CLI side are implemented as fetch-current → mutate → PUT-full-list. Sending `"aliases": []` clears all aliases.

## `DELETE /api/u/me`

```http
DELETE /api/u/me
Authorization: Bearer wd_…

→ 204                (account + aliases + login links all gone)
→ 401                (no/invalid bearer)
```

Cascading removes: aliases, login links, and any AspNet* rows tied to this user. The handle becomes available again immediately for someone else to claim.

## GitHub OAuth - device flow

### `POST /api/auth/github/start`

Kicks off a GitHub device-code flow.

```http
POST /api/auth/github/start
Content-Type: application/json

{ "handle": "sleepless" }     // optional - see below

→ 200 {
  "device_code":      "abc.123...",
  "user_code":        "ABCD-1234",
  "verification_uri": "https://github.com/login/device",
  "interval":          5
}
→ 400 { error: "invalid handle" }
→ 409 { error: "handle taken" }              (handle was supplied and is unavailable)
→ 502 { error: "..." }                       (GitHub API rejected - check client_id)
→ 503 { error: "github auth not configured" }
```

**`handle` semantics:**

- **Supplied** → registration intent. Server validates the handle is free *now* so you don't waste time at github.com only to discover the handle is taken.
- **Omitted** → login intent. Server doesn't know which handle you want yet; it'll figure that out from the GitHub user ID at `/complete`.

### `POST /api/auth/github/complete`

Polled by the CLI until GitHub flips the `device_code` to authorized. Same endpoint handles **register** OR **login** based on whether the GitHub identity is already linked to a user.

```http
POST /api/auth/github/complete
Content-Type: application/json

{
  "device_code":  "abc.123...",     // required, from /start
  "handle":       "sleepless",      // required only if registering, optional if logging in
  "text":         "...",            // optional, registration only
  "avatar_ascii": "[…",       // optional, registration only
  "metadata":     { ... }           // optional, registration only
}

→ 200 { "token": "wd_…", "handle": "..." }
→ 202 { "status": "pending" }                          (poll again after `interval` seconds)
→ 202 { "status": "pending", "slow_down": true }       (poll twice as slowly)
→ 400 { error: "device_code required" }
→ 400 { error: "handle required for new registration" }
→ 401 { error: "expired" }                             (device_code expired - restart from /start)
→ 401 { error: "denied" }                              (user denied at github.com)
→ 409 { error: "handle taken" }
→ 502 { error: "..." }                                 (GitHub API trouble)
→ 503 { error: "github auth not configured" }
```

If the GitHub `id` already maps to a user, the response is a **login** - the server rotates that user's `TokenHash` and returns the new bearer; any extra registration fields in the request are ignored. Otherwise it's a **register** - server creates the user, links the GitHub login, returns the bearer.

## Authentication details

### Bearer token format

`wd_<64 hex chars>` - 32 random bytes hex-encoded, with a `wd_` prefix to make tokens visually distinct.

### How the API verifies it

1. Pull `Authorization: Bearer <token>` header
2. Compute SHA256 of the token bytes
3. Look up the user where `TokenHash` matches the computed hash (indexed for O(log n))
4. Build a `ClaimsPrincipal` with `NameIdentifier = user.Id` so endpoints can resolve via `userManager.GetUserAsync(httpContext.User)`

### Token rotation

A token is rotated only on `register` (new user → new token) or `login` (existing user via GitHub → new token, old one invalidated). There's no refresh endpoint and no expiry; the server-side store is the source of truth.

To "log out everywhere," the closest equivalent is to log in fresh on one machine - that rotates the `TokenHash` and every other machine's stored token stops working.

## Error response shape

Most error responses look like:

```json
{ "error": "<short human-readable reason>" }
```

A few endpoints (validation failures bubbling up from Identity) may include a comma-joined list of multiple reasons. Status codes follow standard semantics; error bodies are advisory, not machine-parseable beyond the JSON envelope.

## What's not here yet (planned)

- `GET /api/random` - random user from `IsHidden=false AND RandomVisible=true`
- Rate limiting per IP for lookups, per token for mutations
- Search / filter endpoint
