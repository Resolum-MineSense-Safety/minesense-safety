using EdgeSynchronizationService.Domain.Model.Aggregates;

namespace EdgeSynchronizationService.Interfaces.REST.Resources;

public record SyncEventResource(
    Guid EventId, string VehicleId, string EventType, DateTimeOffset OccurredAt,
    string Payload, string Status, int Attempts, DateTimeOffset? SynchronizedAt,
    DateTimeOffset? LastAttemptAt, string? LastError)
{
    public static SyncEventResource FromEntity(SyncEvent item) => new(
        item.EventId, item.VehicleId, item.EventType, item.OccurredAt, item.Payload,
        item.Status.ToString(), item.Attempts, item.SynchronizedAt,
        item.LastAttemptAt, item.LastError);
}
