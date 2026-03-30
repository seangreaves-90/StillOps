using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Validation.AspNetCore;

using StillOps.ServiceDefaults;
using StillOps.Web.Components;
using StillOps.Web.Configuration;
using StillOps.Web.Features.AdminPartner.Shell;
using StillOps.Web.Identity;
using StillOps.Web.Identity.Extensions;
using StillOps.Web.Composition;
using StillOps.Web.Identity.Seeding;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ───────────────────────────────────────────────────
// Registers OpenTelemetry (traces + metrics + logs), health checks infrastructure,
// service discovery, and HTTP resilience defaults.
builder.AddServiceDefaults();

// ── Identity infrastructure (OpenIddict + PostgreSQL) ───────────────────────
// Registers ApplicationDbContext (Npgsql + OpenIddict EF Core stores),
// IPasswordHasher<ApplicationUser>, and the embedded OpenIddict authorization
// and validation server. Replaces the Story 1.1 dev-credentials cookie starter.
builder.AddIdentityInfrastructure();

// ── Authentication (cookie + bearer — BFF for browser, token for API clients) ──
// Two schemes serve two audiences:
//   Browser shell (BFF)  → HttpOnly session cookie
//   API clients          → OpenIddict Bearer token (/connect/token)
//
// A policy scheme ("SmartDefault") selects the right scheme per request:
// presence of an Authorization header routes to OpenIddict Bearer validation;
// absence routes to the session cookie. Challenges and forbids always delegate
// to the Cookie scheme, whose events convert /api/* challenges to 401/403 HTTP
// codes instead of HTML redirects so machine clients receive usable responses.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "SmartDefault";
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme("SmartDefault", displayName: null, options =>
    {
        options.ForwardDefaultSelector = ctx =>
            ctx.Request.Headers.ContainsKey("Authorization")
                ? OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.Name = "stillops_shell";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // AC1: unauthenticated /api/* requests → 401 (not redirect).
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return ctx.Response.CompleteAsync();
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };

        // AC1: authenticated-but-wrong-role /api/* requests → 403 (not redirect).
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return ctx.Response.CompleteAsync();
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalShell", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("InternalOperator", "InternalAdmin"));

    options.AddPolicy("InternalAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("InternalAdmin"));
});

// Cascading auth state for Blazor components (AuthorizeView, [Authorize] attribute).
builder.Services.AddCascadingAuthenticationState();

// ── Starter settings (reload source + DI binding) ────────────────────────────
// Web keeps its local file as a seeded fallback for direct dotnet run without AppHost.
// AddStarterSettings() then adds the shared STILLOPS_SETTINGS_FILE path (set by AppHost)
// as the live-editable source. Later configuration sources win for the same key, so the
// shared file takes precedence over the local seed once it exists.
builder.Configuration.AddJsonFile(
    "stillops-operational-settings.json",
    optional: false,
    reloadOnChange: true);

builder.AddStarterSettings();

// ── Health checks (AC 4) ─────────────────────────────────────────────────────
// ServiceDefaults already adds a "self" liveness check tagged ["live"].
// Add the starter topology dependencies consumed by the internal health view:
//   - StillOps.Ingestion via service discovery + /alive
//   - PostgreSQL identity database via EF Core connectivity
builder.Services
    .AddHttpClient(StillOpsIngestionHealthCheck.ClientName, client =>
    {
        client.BaseAddress = new Uri("http://stillops-ingestion");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddServiceDiscovery();

builder.Services
    .AddHealthChecks()
    .AddCheck<StillOpsIngestionHealthCheck>("stillops-ingestion", tags: ["ready"])
    .AddDbContextCheck<ApplicationDbContext>("stillops-identity-db", tags: ["ready"]);

// ── Razor Pages (login, logout, token endpoint) + Blazor ─────────────────────
builder.Services.AddRazorPages();
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery();

// ── Settings writer (Web-only write path) ────────────────────────────────────
// Resolve the write target: shared path when running under AppHost, local
// ContentRoot file when running directly without an orchestrator.
string settingsWritePath =
    builder.Configuration["STILLOPS_SETTINGS_FILE"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "stillops-operational-settings.json");

builder.Services.Configure<StarterSettingsWriterOptions>(options =>
    options.FilePath = settingsWritePath);

builder.Services.AddSingleton<IStarterSettingsWriter, StarterSettingsWriter>();

// ── Module composition (bounded-context service registration) ────────────────
builder.AddStillOpsModules();

// ──────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Dev-only: apply EF Core migrations and seed identity data ────────────────
// Runs before the server starts accepting requests. Requires the AppHost's
// PostgreSQL container to be ready (enforced by WaitFor(identityDb) in AppHost.cs).
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Razor Pages: login, logout, /connect/token
app.MapRazorPages();

// Blazor: all Razor components (shell pages use @attribute [RenderModeInteractiveServer])
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── Protected internal API (AC3 — BFF boundary demonstration) ────────────────
// This stub endpoint proves that /api/internal/* routes are protected by the
// InternalShell policy and return 401/403 for unauthorized callers rather than
// redirecting to the login page. The browser shell calls the server-side
// service layer directly (in-process); this endpoint exists for API clients
// and integration test verification.
app.MapGet("/api/internal/workspace-status",
    (ClaimsPrincipal user) => Results.Ok(new
    {
        status = "ok",
        user = user.Identity!.Name
    }))
    .RequireAuthorization("InternalShell");

// Aspire health + liveness endpoints (Development only by default from ServiceDefaults)
app.MapDefaultEndpoints();

app.Run();
