using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StillOps.Web.Components;
using StillOps.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ───────────────────────────────────────────────────
// Registers OpenTelemetry (traces + metrics + logs), health checks infrastructure,
// service discovery, and HTTP resilience defaults.
builder.AddServiceDefaults();

// ── Authentication (minimal internal-only starter, AC 5) ────────────────────
// Cookie auth with a dev credential.
// Story 1.2 replaces this with OpenIddict OIDC/OAuth flows.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.Name = "stillops_shell";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalShell", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("InternalOperator"));
});

// Cascading auth state for Blazor components (AuthorizeView, [Authorize] attribute).
builder.Services.AddCascadingAuthenticationState();

// ── Reload-capable starter settings (AC 6) ──────────────────────────────────
// stillops-operational-settings.json is the persisted configuration source.
// reloadOnChange: true causes IOptionsMonitor<StarterSettings> consumers to receive
// updated values without restarting the application.
builder.Configuration.AddJsonFile(
    "stillops-operational-settings.json",
    optional: false,
    reloadOnChange: true);

builder.Services.Configure<StarterSettings>(
    builder.Configuration.GetSection(StarterSettings.SectionName));

// ── Health checks (AC 4) ─────────────────────────────────────────────────────
// ServiceDefaults already adds a "self" liveness check tagged ["live"].
// We add the sensor-connectivity readiness concern here as a starter placeholder.
// Tagged ["ready"] so it participates in /health (readiness) but not /alive (liveness).
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "sensor-connectivity",
        () => HealthCheckResult.Degraded(
            "No live device traffic yet — starter topology only. " +
            "Full sensor connectivity arrives in Epic 3."),
        tags: ["ready"]);

// ── Razor Pages (login page) + Blazor ────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery();

// ──────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Razor Pages: login, logout
app.MapRazorPages();

// Blazor: all Razor components (shell pages use @attribute [RenderModeInteractiveServer])
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Aspire health + liveness endpoints (Development only by default from ServiceDefaults)
app.MapDefaultEndpoints();

app.Run();
