using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StillOps.Web.Pages;

public sealed partial class LoginModel(IConfiguration configuration, ILogger<LoginModel> logger)
    : PageModel
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

        var devUsername = configuration["DevCredentials:Username"];
        var devPassword = configuration["DevCredentials:Password"];

        if (string.IsNullOrEmpty(devUsername) || string.IsNullOrEmpty(devPassword))
        {
            LogCredentialsNotConfigured(logger);
            ModelState.AddModelError(string.Empty, "Authentication is not configured. Contact your administrator.");
            return Page();
        }

        if (!string.Equals(Input.Username, devUsername, StringComparison.Ordinal) ||
            !string.Equals(Input.Password, devPassword, StringComparison.Ordinal))
        {
            LogFailedLoginAttempt(logger, Input.Username);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, Input.Username),
            new(ClaimTypes.Role, "InternalOperator"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        LogSuccessfulLogin(logger, Input.Username);

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/shell");
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "DevCredentials are not configured. Add DevCredentials:Username and DevCredentials:Password to appsettings.Development.json.")]
    private static partial void LogCredentialsNotConfigured(ILogger logger);

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
