namespace Whodat.Api.Auth;

public class GithubOptions
{
    public string? ClientId { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);
}
