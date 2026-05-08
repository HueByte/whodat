using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Whodat.Api.Auth;
using Whodat.Api.Data;
using Whodat.Api.Models;

namespace Whodat.Api.Endpoints;

public static class UsersEndpoints
{
    private const int MaxText = 280;
    private const int MaxAscii = 64 * 1024;
    private const int MaxAliases = 5;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/u/{handle}", Lookup);
        app.MapPost("/register", Register);
        app.MapGet("/u/me", Me).RequireAuthorization();
        app.MapPut("/u/me", Update).RequireAuthorization();
        app.MapDelete("/u/me", Delete).RequireAuthorization();
    }

    /// Public lookup. Resolves `handle` first by primary UserName, then by
    /// alias. Hidden users return 404 either way (use `/u/me` to see your own).
    private static async Task<IResult> Lookup(string handle, UserManager<WhodatUser> userManager, WhodatDb db)
    {
        var normalized = Handles.Normalize(handle);
        if (normalized is null) return Results.BadRequest(new { error = "invalid handle" });

        var user = await userManager.Users
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalized.ToUpperInvariant());

        if (user is null)
        {
            // Fall back to alias lookup. Aliases are stored already-normalized.
            var alias = await db.UserAliases
                .Include(a => a.User!).ThenInclude(u => u.Aliases)
                .FirstOrDefaultAsync(a => a.Alias == normalized);
            user = alias?.User;
        }

        if (user is null || user.IsHidden) return Results.NotFound();
        return Results.Ok(EntryDto.From(user));
    }

    private static async Task<IResult> Register(RegisterRequest req, UserManager<WhodatUser> userManager)
    {
        var handle = Handles.Normalize(req.Handle ?? "");
        if (handle is null) return Results.BadRequest(new { error = "invalid handle" });
        if (string.IsNullOrEmpty(req.Password)) return Results.BadRequest(new { error = "password required" });
        if ((req.Text?.Length ?? 0) > MaxText) return Results.BadRequest(new { error = "text too long" });
        if ((req.AvatarAscii?.Length ?? 0) > MaxAscii) return Results.BadRequest(new { error = "avatar too large" });

        var token = Tokens.Generate();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var user = new WhodatUser
        {
            UserName = handle,
            Text = req.Text,
            AvatarAscii = req.AvatarAscii,
            MetadataJson = req.Metadata is null ? null : JsonSerializer.Serialize(req.Metadata),
            TokenHash = Tokens.Hash(token),
            RegisteredAt = now,
            UpdatedAt = now,
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var duplicate = result.Errors.Any(e => e.Code == "DuplicateUserName");
            if (duplicate) return Results.Conflict(new { error = "handle taken" });
            return Results.BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return Results.Ok(new TokenResponse(token, user.UserName!));
    }

    private static async Task<IResult> Me(HttpContext ctx, UserManager<WhodatUser> userManager)
    {
        // Reload via the queryable so Aliases get populated; UserManager.GetUserAsync
        // doesn't hydrate navigation properties.
        var id = userManager.GetUserId(ctx.User);
        if (id is null) return Results.Unauthorized();
        var user = await userManager.Users
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? Results.Unauthorized() : Results.Ok(EntryDto.From(user));
    }

    private static async Task<IResult> Update(
        UpdateRequest req,
        HttpContext ctx,
        UserManager<WhodatUser> userManager,
        WhodatDb db)
    {
        var id = userManager.GetUserId(ctx.User);
        if (id is null) return Results.Unauthorized();
        var user = await userManager.Users
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return Results.Unauthorized();

        if (req.Text is not null)
        {
            if (req.Text.Length > MaxText) return Results.BadRequest(new { error = "text too long" });
            user.Text = req.Text;
        }
        if (req.AvatarAscii is not null)
        {
            if (req.AvatarAscii.Length > MaxAscii) return Results.BadRequest(new { error = "avatar too large" });
            user.AvatarAscii = req.AvatarAscii;
        }
        if (req.Metadata is not null)
        {
            user.MetadataJson = req.Metadata.Count == 0 ? null : JsonSerializer.Serialize(req.Metadata);
        }
        if (req.IsHidden is { } hidden)
        {
            user.IsHidden = hidden;
            // Hiding cascades to undiscoverable. We don't auto-restore
            // RandomVisible on unhide — user re-opts via `discoverable`.
            if (hidden) user.RandomVisible = false;
        }
        if (req.RandomVisible is { } randomVisible)
        {
            user.RandomVisible = randomVisible;
        }
        if (req.Aliases is not null)
        {
            var aliasError = await ApplyAliases(req.Aliases, user, userManager, db);
            if (aliasError is not null) return aliasError;
        }

        user.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Results.Problem(string.Join(", ", result.Errors.Select(e => e.Description)));

        return Results.Ok(EntryDto.From(user));
    }

    /// Validate + apply a replace-all alias list. Returns null on success,
    /// or an IResult representing the validation failure.
    private static async Task<IResult?> ApplyAliases(
        List<string> raw,
        WhodatUser user,
        UserManager<WhodatUser> userManager,
        WhodatDb db)
    {
        if (raw.Count > MaxAliases)
            return Results.BadRequest(new { error = $"max {MaxAliases} aliases" });

        // Normalize, dedupe, validate format.
        var normalized = new List<string>(raw.Count);
        foreach (var a in raw)
        {
            var n = Handles.Normalize(a);
            if (n is null)
                return Results.BadRequest(new { error = $"invalid alias '{a}'" });
            if (n == user.UserName)
                return Results.BadRequest(new { error = $"alias '{n}' equals your handle" });
            if (!normalized.Contains(n)) normalized.Add(n);
        }

        // Aliases must not collide with anyone else's handle.
        if (normalized.Count > 0)
        {
            var upper = normalized.Select(n => n.ToUpperInvariant()).ToList();
            var collidingHandle = await userManager.Users
                .AnyAsync(u => upper.Contains(u.NormalizedUserName!) && u.Id != user.Id);
            if (collidingHandle)
                return Results.Conflict(new { error = "an alias collides with another user's handle" });

            // Aliases must not collide with anyone else's existing alias.
            var otherAliasOwners = await db.UserAliases
                .Where(a => normalized.Contains(a.Alias) && a.UserId != user.Id)
                .Select(a => a.Alias)
                .FirstOrDefaultAsync();
            if (otherAliasOwners is not null)
                return Results.Conflict(new { error = $"alias '{otherAliasOwners}' is taken" });
        }

        // Replace-all: drop everything currently attached to this user, insert the new set.
        db.UserAliases.RemoveRange(user.Aliases);
        user.Aliases.Clear();
        foreach (var n in normalized)
        {
            var alias = new UserAlias { UserId = user.Id, Alias = n };
            user.Aliases.Add(alias);
        }
        return null;
    }

    private static async Task<IResult> Delete(HttpContext ctx, UserManager<WhodatUser> userManager)
    {
        var user = await userManager.GetUserAsync(ctx.User);
        if (user is null) return Results.Unauthorized();

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded
            ? Results.NoContent()
            : Results.Problem(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
