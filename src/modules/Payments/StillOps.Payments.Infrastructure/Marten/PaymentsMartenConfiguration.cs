namespace StillOps.Payments.Infrastructure.Marten;

/// <summary>
/// Marten event store and document store configuration for the Payments bounded context.
/// Targets the "payments" PostgreSQL schema.
/// Streams, projections, and document types will be registered by Epic 6 stories.
/// </summary>
public static class PaymentsMartenConfiguration
{
    public const string SchemaName = "payments";
}
