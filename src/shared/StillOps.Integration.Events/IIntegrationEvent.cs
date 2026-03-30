namespace StillOps.Integration.Events;

/// <summary>
/// Marker interface for cross-bounded-context integration events.
/// All integration events must live in this assembly.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string? CorrelationId { get; }
}
