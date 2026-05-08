using System.Text.Json;
using System.Text.Json.Serialization;

namespace Whodat.Api.Models;

public record EntryDto(
    string Handle,
    string? Text,
    [property: JsonPropertyName("avatar_ascii")] string? AvatarAscii,
    Dictionary<string, string> Metadata,
    [property: JsonPropertyName("registered_at")] long RegisteredAt,
    [property: JsonPropertyName("updated_at")] long UpdatedAt)
{
    public static EntryDto From(WhodatUser u) => new(
        u.UserName ?? "",
        u.Text,
        u.AvatarAscii,
        string.IsNullOrEmpty(u.MetadataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(u.MetadataJson) ?? new(),
        u.RegisteredAt,
        u.UpdatedAt);
}

public record RegisterRequest(
    string Handle,
    string? Password,
    string? Text,
    [property: JsonPropertyName("avatar_ascii")] string? AvatarAscii,
    Dictionary<string, string>? Metadata);

/// Returned on every register / login. Includes the handle so login flows
/// (where the CLI doesn't know the handle ahead of time) can save it locally
/// without a follow-up call.
public record TokenResponse(string Token, string Handle);

public record UpdateRequest(
    string? Text,
    [property: JsonPropertyName("avatar_ascii")] string? AvatarAscii,
    Dictionary<string, string>? Metadata);
