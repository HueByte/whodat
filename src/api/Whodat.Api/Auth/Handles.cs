using System.Text.RegularExpressions;

namespace Whodat.Api.Auth;

public static partial class Handles
{
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,30}[a-z0-9])?$")]
    private static partial Regex Valid();

    public static string? Normalize(string raw)
    {
        var lower = raw.Trim().ToLowerInvariant();
        return Valid().IsMatch(lower) ? lower : null;
    }
}
