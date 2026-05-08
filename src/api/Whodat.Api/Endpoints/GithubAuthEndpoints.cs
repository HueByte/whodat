using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Whodat.Api.Auth;
using Whodat.Api.Data;
using Whodat.Api.Models;

namespace Whodat.Api.Endpoints;

/// GitHub OAuth via the device-code flow. The CLI calls /start to get a
/// `user_code` for the user to type at github.com/login/device, then polls
/// /complete until GitHub authorizes. On success, the registration is
/// persisted with auth_kind = github and the bearer token is returned.
public static class GithubAuthEndpoints
{
    public const string HttpClientName = "github";

    private const int MaxText = 280;
    private const int MaxAscii = 64 * 1024;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/github/start", Start);
        app.MapPost("/auth/github/complete", Complete);
    }

    private static async Task<IResult> Start(
        StartRequest req,
        IHttpClientFactory httpFactory,
        IOptions<GithubOptions> options,
        WhodatDb db)
    {
        if (!options.Value.IsConfigured)
            return Disabled();

        var handle = Handles.Normalize(req.Handle ?? "");
        if (handle is null) return Results.BadRequest(new { error = "invalid handle" });
        if (await db.Users.AnyAsync(u => u.Handle == handle))
            return Results.Conflict(new { error = "handle taken" });

        using var http = httpFactory.CreateClient(HttpClientName);
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", options.Value.ClientId!),
            new KeyValuePair<string, string>("scope", "read:user"),
        });
        var resp = await http.PostAsync("https://github.com/login/device/code", form);
        if (!resp.IsSuccessStatusCode)
            return Results.Json(new { error = "github device request failed" }, statusCode: 502);

        var device = await resp.Content.ReadFromJsonAsync<GithubDeviceResponse>();
        if (device is null || string.IsNullOrEmpty(device.DeviceCode))
            return Results.Json(new { error = device?.Error ?? "invalid github response" }, statusCode: 502);

        return Results.Ok(new StartResponse(
            device.DeviceCode!,
            device.UserCode ?? "",
            device.VerificationUri ?? "",
            device.ExpiresIn,
            device.Interval));
    }

    private static async Task<IResult> Complete(
        CompleteRequest req,
        IHttpClientFactory httpFactory,
        IOptions<GithubOptions> options,
        WhodatDb db)
    {
        if (!options.Value.IsConfigured)
            return Disabled();

        var handle = Handles.Normalize(req.Handle ?? "");
        if (handle is null) return Results.BadRequest(new { error = "invalid handle" });
        if (string.IsNullOrEmpty(req.DeviceCode)) return Results.BadRequest(new { error = "device_code required" });
        if ((req.Text?.Length ?? 0) > MaxText) return Results.BadRequest(new { error = "text too long" });
        if ((req.AvatarAscii?.Length ?? 0) > MaxAscii) return Results.BadRequest(new { error = "avatar too large" });

        using var http = httpFactory.CreateClient(HttpClientName);
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", options.Value.ClientId!),
            new KeyValuePair<string, string>("device_code", req.DeviceCode),
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
        });
        var tokenResp = await http.PostAsync("https://github.com/login/oauth/access_token", form);
        var tokenBody = await tokenResp.Content.ReadFromJsonAsync<GithubTokenResponse>();
        if (tokenBody is null)
            return Results.Json(new { error = "invalid github response" }, statusCode: 502);

        // Device flow returns 200 either way; the polling state is in the JSON body.
        if (!string.IsNullOrEmpty(tokenBody.Error))
        {
            return tokenBody.Error switch
            {
                "authorization_pending" => Results.Json(new { status = "pending" }, statusCode: 202),
                "slow_down" => Results.Json(new { status = "pending", slow_down = true }, statusCode: 202),
                "expired_token" => Results.Json(new { error = "expired" }, statusCode: 401),
                "access_denied" => Results.Json(new { error = "denied" }, statusCode: 401),
                _ => Results.Json(new { error = tokenBody.Error }, statusCode: 400),
            };
        }

        if (string.IsNullOrEmpty(tokenBody.AccessToken))
            return Results.Json(new { error = "no access token" }, statusCode: 502);

        // Resolve the GitHub user behind the token.
        var userReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenBody.AccessToken);
        var userResp = await http.SendAsync(userReq);
        if (!userResp.IsSuccessStatusCode)
            return Results.Json(new { error = "github user lookup failed" }, statusCode: 502);
        var user = await userResp.Content.ReadFromJsonAsync<GithubUser>();
        if (user is null || user.Id == 0)
            return Results.Json(new { error = "invalid github user" }, statusCode: 502);

        // One github account, one handle.
        if (await db.Users.AnyAsync(u => u.GithubId == user.Id))
            return Results.Conflict(new { error = "github account already registered" });
        if (await db.Users.AnyAsync(u => u.Handle == handle))
            return Results.Conflict(new { error = "handle taken" });

        var bearer = Tokens.Generate();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        db.Users.Add(new UserEntry
        {
            Handle = handle,
            Text = req.Text,
            AvatarAscii = req.AvatarAscii,
            MetadataJson = req.Metadata is null ? null : JsonSerializer.Serialize(req.Metadata),
            AuthKind = AuthKind.Github,
            GithubId = user.Id,
            TokenHash = Tokens.Hash(bearer),
            RegisteredAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();

        return Results.Ok(new TokenResponse(bearer));
    }

    private static IResult Disabled() =>
        Results.Json(new { error = "github auth not configured" }, statusCode: 503);

    public record StartRequest(string Handle);

    public record StartResponse(
        [property: JsonPropertyName("device_code")] string DeviceCode,
        [property: JsonPropertyName("user_code")] string UserCode,
        [property: JsonPropertyName("verification_uri")] string VerificationUri,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        int Interval);

    public record CompleteRequest(
        [property: JsonPropertyName("device_code")] string DeviceCode,
        string Handle,
        string? Text,
        [property: JsonPropertyName("avatar_ascii")] string? AvatarAscii,
        Dictionary<string, string>? Metadata);

    private record GithubDeviceResponse(
        [property: JsonPropertyName("device_code")] string? DeviceCode,
        [property: JsonPropertyName("user_code")] string? UserCode,
        [property: JsonPropertyName("verification_uri")] string? VerificationUri,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        int Interval,
        string? Error);

    private record GithubTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType,
        string? Scope,
        string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);

    private record GithubUser(long Id, string Login);
}
