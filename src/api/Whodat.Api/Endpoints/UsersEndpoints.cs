using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Whodat.Api.Auth;
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
        app.MapGet("/u/me", Me).RequireAuthorization();
        app.MapPut("/u/me", Update).RequireAuthorization();
        app.MapDelete("/u/me", Delete).RequireAuthorization();
    }

    private static async Task<IResult> Lookup(string handle, UserManager<WhodatUser> userManager)
    {
        var normalized = Handles.Normalize(handle);
        if (normalized is null) return Results.BadRequest(new { error = "invalid handle" });

        var user = await userManager.FindByNameAsync(normalized);
        return user is null ? Results.NotFound() : Results.Ok(EntryDto.From(user));
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
            // DuplicateUserName is the only "user-fixable" failure we expect.
            var duplicate = result.Errors.Any(e => e.Code == "DuplicateUserName");
            if (duplicate) return Results.Conflict(new { error = "handle taken" });
            return Results.BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return Results.Ok(new TokenResponse(token, user.UserName!));
    }

    private static async Task<IResult> Me(HttpContext ctx, UserManager<WhodatUser> userManager)
    {
        var user = await userManager.GetUserAsync(ctx.User);
        return user is null ? Results.Unauthorized() : Results.Ok(EntryDto.From(user));
    }

    private static async Task<IResult> Update(UpdateRequest req, HttpContext ctx, UserManager<WhodatUser> userManager)
    {
        var user = await userManager.GetUserAsync(ctx.User);
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
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Results.Problem(string.Join(", ", result.Errors.Select(e => e.Description)));

        return Results.Ok(EntryDto.From(user));
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
