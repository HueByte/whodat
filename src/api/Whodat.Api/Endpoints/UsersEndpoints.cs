using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Whodat.Api.Auth;
using Whodat.Api.Data;
using Whodat.Api.Models;

namespace Whodat.Api.Endpoints;

public static class UsersEndpoints
{
    private const int MaxText = 280;
    private const int MaxAscii = 64 * 1024;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/u/{handle}", Lookup);
        app.MapPost("/register", Register);
        app.MapPut("/u/me", Update);
        app.MapDelete("/u/me", Delete);
    }

    private static async Task<IResult> Lookup(string handle, WhodatDb db)
    {
        var normalized = Handles.Normalize(handle);
        if (normalized is null) return Results.BadRequest(new { error = "invalid handle" });

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Handle == normalized);
        return user is null ? Results.NotFound() : Results.Ok(EntryDto.From(user));
    }

    private static async Task<IResult> Register(RegisterRequest req, WhodatDb db)
    {
        var handle = Handles.Normalize(req.Handle ?? "");
        if (handle is null) return Results.BadRequest(new { error = "invalid handle" });
        if (string.IsNullOrEmpty(req.Password)) return Results.BadRequest(new { error = "password required" });
        if ((req.Text?.Length ?? 0) > MaxText) return Results.BadRequest(new { error = "text too long" });
        if ((req.AvatarAscii?.Length ?? 0) > MaxAscii) return Results.BadRequest(new { error = "avatar too large" });

        if (await db.Users.AnyAsync(u => u.Handle == handle))
            return Results.Conflict(new { error = "handle taken" });

        var token = Tokens.Generate();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        db.Users.Add(new UserEntry
        {
            Handle = handle,
            Text = req.Text,
            AvatarAscii = req.AvatarAscii,
            MetadataJson = req.Metadata is null ? null : JsonSerializer.Serialize(req.Metadata),
            AuthKind = AuthKind.Password,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            TokenHash = Tokens.Hash(token),
            RegisteredAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();

        return Results.Ok(new TokenResponse(token));
    }

    private static async Task<IResult> Update(UpdateRequest req, HttpContext ctx, WhodatDb db)
    {
        var user = await Authenticate(ctx, db);
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

        user.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await db.SaveChangesAsync();
        return Results.Ok(EntryDto.From(user));
    }

    private static async Task<IResult> Delete(HttpContext ctx, WhodatDb db)
    {
        var user = await Authenticate(ctx, db);
        if (user is null) return Results.Unauthorized();

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<UserEntry?> Authenticate(HttpContext ctx, WhodatDb db)
    {
        var token = Tokens.Extract(ctx);
        if (token is null) return null;
        var hash = Tokens.Hash(token);
        return await db.Users.FirstOrDefaultAsync(u => u.TokenHash == hash);
    }
}
