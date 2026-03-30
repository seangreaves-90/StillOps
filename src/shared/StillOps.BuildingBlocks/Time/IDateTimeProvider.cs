namespace StillOps.BuildingBlocks.Time;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
