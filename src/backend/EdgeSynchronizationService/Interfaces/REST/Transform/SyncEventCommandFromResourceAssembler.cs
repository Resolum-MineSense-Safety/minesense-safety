using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Interfaces.REST.Resources;

namespace EdgeSynchronizationService.Interfaces.REST.Transform;

public static class SyncEventCommandFromResourceAssembler
{
    public static EnqueueSyncEventCommand ToCommand(EnqueueSyncEventResource resource) =>
        new(resource.EventId, resource.VehicleId, resource.EventType,
            resource.OccurredAt, resource.Payload.GetRawText());
}
