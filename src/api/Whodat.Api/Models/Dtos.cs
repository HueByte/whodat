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
    public static EntryDto From(UserEntry e) => new(
        e.Handle,
        e.Text,
        e.AvatarAscii,
        string.IsNullOrEmpty(e.MetadataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(e.MetadataJson) ?? new(),
        e.RegisteredAt,
        e.UpdatedAt);
}

public record RegisterRequest(
    string Handle,
    string? Password,
    string? Text,
    [property: JsonPropertyName("avatar_ascii")] string? AvatarAscii,
    Dictionary<string, string>? Metadata);

public record TokenResponse(string Token);

public record UpdateRequest(
    string? Text,
    [property: JsonPropertyName("avatar_ascii")] string? AvatarAscii,
    Dictionary<string, string>? Metadata);
