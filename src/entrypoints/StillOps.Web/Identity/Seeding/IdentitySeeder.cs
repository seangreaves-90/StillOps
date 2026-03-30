using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using StillOps.Web.Identity.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace StillOps.Web.Identity.Seeding;

/// <summary>
/// Seeds development identity data: operator/admin users and the OpenIddict
/// application registration. Only runs in Development environment.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Apply any pending EF Core migrations before seeding.
        await context.Database.MigrateAsync();

        await SeedUsersAsync(services, context);
        await SeedOpenIddictAsync(services);
    }

    private static async Task SeedUsersAsync(
        IServiceProvider services,
        ApplicationDbContext context)
    {
        var hasher = services.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        // Idempotent per-username: only insert a user when it does not already exist.
        // This handles both fresh databases and existing dev databases being upgraded.
        var seeds = new[]
        {
            // Dev credentials — intentionally weak, replaced by real credentials in production.
            ("operator", "InternalOperator"),
            ("admin",    "InternalAdmin"),
            // Dev-only viewer: authenticated but excluded from InternalShell policy.
            // Used in integration tests to verify 403 Forbidden for wrong-role callers.
            ("viewer",   "ViewerRole"),
        };

        var anyAdded = false;
        foreach (var (username, role) in seeds)
        {
            if (!await context.Users.AnyAsync(u => u.Username == username))
            {
                var user = new ApplicationUser
                {
                    Username = username,
                    PasswordHash = string.Empty,
                    Role = role
                };
                user.PasswordHash = hasher.HashPassword(user, "dev-only-change-me");
                context.Users.Add(user);
                anyAdded = true;
            }
        }

        if (anyAdded) await context.SaveChangesAsync();
    }

    private static async Task SeedOpenIddictAsync(IServiceProvider services)
    {
        var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();

        // Register the api.internal scope for protected backend capabilities.
        if (await scopeManager.FindByNameAsync("api.internal") is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api.internal",
                DisplayName = "StillOps Internal API access"
            });
        }

        // Register stillops-web as an OpenIddict client application.
        // Password flow is enabled here for dev/learning. Production should use
        // authorization code flow with PKCE.
        if (await applicationManager.FindByClientIdAsync("stillops-web") is null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "stillops-web",
                DisplayName = "StillOps Web Application",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.ResponseTypes.Token,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api.internal",
                }
            });
        }
    }
}
