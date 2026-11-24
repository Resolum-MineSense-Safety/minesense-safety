using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Interfaces.REST.Resources;

namespace EdgeSynchronizationService.Interfaces.REST.Transform;

public static class SynchronizationBatchAssembler
{
    public static SubmitSynchronizationBatchCommand ToCommand(SubmitSynchronizationBatchResource resource) =>
        new(resource.BatchId, resource.EdgeNodeId, resource.DeviceId, resource.SequenceNumber,
            resource.CapturedAt, (resource.Events ?? []).Select(ToEvent).ToList());

    public static SynchronizationBatchResource ToResource(SynchronizationBatch batch) =>
        new(batch.BatchId, batch.EdgeNodeId, batch.DeviceId, batch.SequenceNumber,
            batch.CapturedAt, batch.ReceivedAt, batch.Status.ToString(),
            batch.Events.Select(item => new SynchronizationEventResource(
                item.EventId, item.OccurredAt, item.EventType, item.Payload)).ToList());

    private static SynchronizationEvent ToEvent(SynchronizationEventResource resource) =>
        new(resource.EventId, resource.OccurredAt, resource.EventType, resource.Payload);
}
