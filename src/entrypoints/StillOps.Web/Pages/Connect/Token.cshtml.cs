using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using StillOps.Web.Identity;
using StillOps.Web.Identity.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace StillOps.Web.Pages.Connect;

/// <summary>
/// Handles POST /connect/token — the OpenIddict token endpoint.
///
/// For browser users, the login page (Login.cshtml.cs) issues a session cookie directly.
/// This endpoint is for API clients (curl, HttpClient) that need a Bearer token
/// to call /api/internal/ endpoints — demonstrating the OpenIddict authorization server flow.
///
/// Password grant is enabled for dev/learning. Production should use authorization code
/// flow with PKCE from a registered client application.
/// </summary>
[IgnoreAntiforgeryToken]
public sealed partial class TokenModel(
    ApplicationDbContext context,
    IPasswordHasher<ApplicationUser> hasher,
    ILogger<TokenModel> logger) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenIddict server request cannot be retrieved.");

        if (!request.IsPasswordGrantType())
        {
            return Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                        Errors.UnsupportedGrantType,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Only the password grant type is supported."
                }),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!)
                == PasswordVerificationResult.Failed)
        {
            LogFailedTokenRequest(logger, request.Username);
            return Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                        Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Invalid username or password."
                }),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        LogSuccessfulTokenRequest(logger, user.Username);

        // Build a ClaimsPrincipal carrying subject, name, and role.
        // Destinations control which token types (access/identity) each claim appears in.
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(Claims.Subject, user.Id.ToString());
        identity.AddClaim(Claims.Name, user.Username);
        identity.AddClaim(Claims.Role, user.Role);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<string> GetDestinations(Claim claim) =>
        claim.Type switch
        {
            // Name and role appear in both tokens so they are usable client-side and server-side.
            Claims.Name or Claims.Role =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            _ =>
                [Destinations.AccessToken]
        };

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed token request for username: {Username}")]
    private static partial void LogFailedTokenRequest(ILogger logger, string? username);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Token issued for {Username}")]
    private static partial void LogSuccessfulTokenRequest(ILogger logger, string username);
}
