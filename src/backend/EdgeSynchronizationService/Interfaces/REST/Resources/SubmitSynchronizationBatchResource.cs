using System.Text.Json.Serialization;

namespace EdgeSynchronizationService.Interfaces.REST.Resources;

public sealed record SubmitSynchronizationBatchResource(
    [property: JsonRequired] Guid BatchId,
    [property: JsonRequired] Guid EdgeNodeId,
    [property: JsonRequired] Guid DeviceId,
    [property: JsonRequired] long SequenceNumber,
    [property: JsonRequired] DateTimeOffset CapturedAt,
    [property: JsonRequired] IReadOnlyCollection<SynchronizationEventResource> Events);
