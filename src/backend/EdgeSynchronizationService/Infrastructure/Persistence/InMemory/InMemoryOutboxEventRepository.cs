using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.ValueObjects;
using EdgeSynchronizationService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace EdgeSynchronizationService.Infrastructure.Persistence.InMemory;

public class InMemoryOutboxEventRepository : InMemoryRepository<SyncEvent>, IOutboxEventRepository
{
    public Task<SyncEvent?> FindByEventIdAsync(Guid eventId) =>
        Task.FromResult(Store.Values.FirstOrDefault(item => item.EventId == eventId));

    public Task<IEnumerable<SyncEvent>> FindPendingAsync(string? vehicleId, int batchSize) =>
        Task.FromResult<IEnumerable<SyncEvent>>(Store.Values
            .Where(item => item.Status is SyncEventStatus.Pending or SyncEventStatus.Failed)
            .Where(item => vehicleId is null || item.VehicleId == vehicleId)
            .OrderBy(item => item.OccurredAt).Take(batchSize).ToList());

    public Task<IEnumerable<SyncEvent>> FindByVehicleAsync(string vehicleId) =>
        Task.FromResult<IEnumerable<SyncEvent>>(Store.Values
            .Where(item => item.VehicleId == vehicleId)
            .OrderByDescending(item => item.OccurredAt).ToList());
}
