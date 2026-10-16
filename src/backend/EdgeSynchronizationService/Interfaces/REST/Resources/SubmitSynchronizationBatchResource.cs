using System.Text.Json;

namespace EdgeSynchronizationService.Interfaces.REST.Resources;

public sealed record SubmitSynchronizationBatchResource(
    Guid BatchId,
    Guid EdgeNodeId,
    Guid DeviceId,
    long SequenceNumber,
    DateTimeOffset CapturedAt,
    IReadOnlyCollection<SynchronizationEventResource> Events);
