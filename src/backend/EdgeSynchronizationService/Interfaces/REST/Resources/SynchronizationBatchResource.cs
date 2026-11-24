using System.Text.Json;

namespace EdgeSynchronizationService.Interfaces.REST.Resources;

public sealed record SynchronizationBatchResource(
    Guid BatchId,
    Guid EdgeNodeId,
    Guid DeviceId,
    long SequenceNumber,
    DateTimeOffset CapturedAt,
    DateTimeOffset ReceivedAt,
    string Status,
    IReadOnlyCollection<SynchronizationEventResource> Events);

public sealed record SynchronizationEventResource(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string EventType,
    JsonElement Payload);
