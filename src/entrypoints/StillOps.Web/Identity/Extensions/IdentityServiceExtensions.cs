using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StillOps.Web.Identity.Models;

namespace StillOps.Web.Identity.Extensions;

/// <summary>
/// Wires all identity infrastructure: DbContext, password hashing, and
/// the embedded OpenIddict authorization/validation server.
/// </summary>
public static class IdentityServiceExtensions
{
    public static IHostApplicationBuilder AddIdentityInfrastructure(
        this IHostApplicationBuilder builder)
    {
        // EF Core DbContext backed by the Aspire-provisioned PostgreSQL database.
        // The connection string is injected by Aspire via WithReference(identityDb)
        // in AppHost.cs as ConnectionStrings:stillops-identity.
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("stillops-identity")
                ?? throw new InvalidOperationException(
                    "Connection string 'stillops-identity' not found. " +
                    "Ensure the AppHost has a PostgreSQL database named 'stillops-identity' " +
                    "referenced with WithReference().");

            options.UseNpgsql(connectionString);
        });

        // ASP.NET Core built-in password hasher — no full Identity framework required.
        builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

        // OpenIddict authorization server (embedded in StillOps.Web — modular monolith phase).
        // Story 1.2 enables password flow for learning. Production target: authorization code
        // flow with PKCE. Extractable to a dedicated identity service in a later phase.
        builder.Services.AddOpenIddict()
            .AddCore(options => options
                .UseEntityFrameworkCore()
                .UseDbContext<ApplicationDbContext>())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token");

                // Password flow: dev/learning only.
                // Allows curl/Postman to obtain tokens for manual AC verification.
                options.AllowPasswordFlow();
                options.AcceptAnonymousClients();

                // Development certificates — replace with real certs before production.
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // Passes /connect/token POST requests through to Pages/Connect/Token.cshtml.cs.
                var aspNetCore = options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();

                // AppHost integration tests exercise the local HTTP endpoint exposed by the
                // Aspire dev topology. OpenIddict rejects non-HTTPS token requests by default,
                // which surfaces as 400 BadRequest on /connect/token.
                if (builder.Environment.IsDevelopment())
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                // Validate Bearer tokens issued by the embedded authorization server.
                // Used by /api/internal/ endpoints to accept tokens from API clients.
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return builder;
    }
}
