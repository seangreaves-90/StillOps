using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StillOps.Web.Pages;

public sealed partial class LogoutModel(ILogger<LogoutModel> logger) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        var name = User.Identity?.Name;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        LogUserSignedOut(logger, name ?? "unknown");
        return LocalRedirect("/login");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "User signed out: {Name}")]
    private static partial void LogUserSignedOut(ILogger logger, string name);
}
