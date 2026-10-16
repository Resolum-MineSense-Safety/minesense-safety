using EdgeSynchronizationService.Domain.Model.Aggregates;

namespace EdgeSynchronizationService.Domain.Services;

public interface ISynchronizationQueryService
{
    Task<IEnumerable<SyncEvent>> GetByVehicleAsync(string vehicleId);
}
