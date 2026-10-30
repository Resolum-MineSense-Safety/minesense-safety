using System.Text.Json;

namespace EdgeSynchronizationService.Domain.Model.Commands;

public sealed record SubmitSynchronizationBatchCommand(
    Guid BatchId,
    Guid EdgeNodeId,
    Guid DeviceId,
    long SequenceNumber,
    DateTimeOffset CapturedAt,
    IReadOnlyCollection<SynchronizationEvent> Events);

public sealed record SynchronizationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string EventType,
    JsonElement Payload);
