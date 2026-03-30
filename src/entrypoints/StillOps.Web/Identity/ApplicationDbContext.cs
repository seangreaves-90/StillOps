using Microsoft.EntityFrameworkCore;
using StillOps.Web.Identity.Models;

namespace StillOps.Web.Identity;

/// <summary>
/// EF Core context for StillOps identity data.
/// Owns the ApplicationUser table and all OpenIddict entity tables
/// (applications, authorizations, scopes, tokens) via UseOpenIddict().
///
/// Schema: identity (infrastructure/support schema per architecture data boundaries).
/// Extractable to a dedicated Identity module project in a future story.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Registers OpenIddict entity type configurations:
        // OpenIddictApplication, OpenIddictAuthorization, OpenIddictScope, OpenIddictToken.
        modelBuilder.UseOpenIddict();

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users", "identity");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(64).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(64).IsRequired();
        });
    }
}
