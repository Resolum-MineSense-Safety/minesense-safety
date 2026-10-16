namespace EdgeSynchronizationService.Domain.Model.Commands;

public record EnqueueSyncEventCommand(
    Guid EventId,
    string VehicleId,
    string EventType,
    DateTimeOffset OccurredAt,
    string Payload);
