using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StillOps.Web.Identity;
using StillOps.Web.Identity.Models;

namespace StillOps.Web.Pages;

public sealed partial class LoginModel(
    ApplicationDbContext context,
    IPasswordHasher<ApplicationUser> hasher,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == Input.Username);

        if (user is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password)
                == PasswordVerificationResult.Failed)
        {
            LogFailedLoginAttempt(logger, Input.Username);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // The browser receives only this HttpOnly session cookie — the raw
        // OpenIddict token stays server-side (BFF pattern, AC3).
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        LogSuccessfulLogin(logger, user.Username);

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/shell");
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed login attempt for username: {Username}")]
    private static partial void LogFailedLoginAttempt(ILogger logger, string username);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Successful login for {Username}")]
    private static partial void LogSuccessfulLogin(ILogger logger, string username);

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
