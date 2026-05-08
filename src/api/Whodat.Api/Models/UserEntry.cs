using System.ComponentModel.DataAnnotations;

namespace Whodat.Api.Models;

public enum AuthKind
{
    Password,
    Github,
}

public class UserEntry
{
    [Key, MaxLength(32)]
    public string Handle { get; set; } = "";

    public string? Text { get; set; }
    public string? AvatarAscii { get; set; }
    public string? MetadataJson { get; set; }

    public AuthKind AuthKind { get; set; }
    public string? PasswordHash { get; set; }
    public long? GithubId { get; set; }

    public string TokenHash { get; set; } = "";

    public long RegisteredAt { get; set; }
    public long UpdatedAt { get; set; }
}
