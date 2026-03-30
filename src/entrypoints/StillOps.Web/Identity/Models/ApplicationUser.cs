namespace StillOps.Web.Identity.Models;

/// <summary>
/// Represents an internal operator or administrator identity.
/// Deliberately minimal — only the fields needed for Story 1.2 role-based access.
/// </summary>
public sealed class ApplicationUser
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Role claim value: "InternalOperator" or "InternalAdmin".
    /// Maps directly to the InternalShell authorization policy.
    /// </summary>
    public required string Role { get; set; }
}
