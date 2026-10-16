using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Commands;

namespace EdgeSynchronizationService.Domain.Services;

public interface ISynchronizationCommandService
{
    Task<(SyncEvent Event, bool AlreadyExists)> EnqueueAsync(EnqueueSyncEventCommand command);
    Task<IEnumerable<SyncEvent>> SynchronizeAsync(SynchronizePendingEventsCommand command);
}
