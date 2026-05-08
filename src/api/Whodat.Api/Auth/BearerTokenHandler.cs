using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whodat.Api.Models;

namespace Whodat.Api.Auth;

public class BearerTokenOptions : AuthenticationSchemeOptions { }

/// Resolves an `Authorization: Bearer <token>` header to a WhodatUser by
/// hashing the token and looking it up via the indexed TokenHash column.
/// Identity's standard auth handlers (cookies / JWT) don't fit our
/// CLI-issued long-lived bearer model, so we authenticate manually.
public class BearerTokenHandler(
    IOptionsMonitor<BearerTokenOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    UserManager<WhodatUser> userManager) : AuthenticationHandler<BearerTokenOptions>(options, logger, encoder)
{
    public const string SchemeName = "Bearer";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.Ordinal))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..];
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        var hash = Tokens.Hash(token);
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.TokenHash == hash);
        if (user is null)
            return AuthenticateResult.Fail("invalid token");

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
        ], Scheme.Name);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
