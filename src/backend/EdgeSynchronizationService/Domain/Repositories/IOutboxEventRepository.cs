using EdgeSynchronizationService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Repositories;

namespace EdgeSynchronizationService.Domain.Repositories;

public interface IOutboxEventRepository : IBaseRepository<SyncEvent>
{
    Task<SyncEvent?> FindByEventIdAsync(Guid eventId);
    Task<IEnumerable<SyncEvent>> FindPendingAsync(string? vehicleId, int batchSize);
    Task<IEnumerable<SyncEvent>> FindByVehicleAsync(string vehicleId);
}
